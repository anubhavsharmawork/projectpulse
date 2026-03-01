using Application.Common.Interfaces;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace Application.Workflows.Commands
{
    public record UpdateProjectWorkflowCommand(
        Guid ProjectId,
        string Name,
        List<UpdateWorkflowStateDto> States) : IRequest<Guid>;

    public record UpdateWorkflowStateDto(
        string Name,
        int Order,
        string Color,
        bool IsInitial,
        bool IsFinal,
        List<string>? AllowedTransitionNames,
        List<string>? RequiredFields,
        bool NotifyOnEntry);

    public class UpdateProjectWorkflowHandler : IRequestHandler<UpdateProjectWorkflowCommand, Guid>
    {
        private readonly IAppDbContext _db;
        private readonly IHttpContextAccessor _http;

        public UpdateProjectWorkflowHandler(IAppDbContext db, IHttpContextAccessor http)
        {
            _db = db;
            _http = http;
        }

        public async Task<Guid> Handle(UpdateProjectWorkflowCommand request, CancellationToken cancellationToken)
        {
            var userIdClaim = _http.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userId = Guid.TryParse(userIdClaim, out var id) ? id : Guid.Empty;

            var project = await _db.Projects
                .FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken)
                ?? throw new InvalidOperationException("Project not found");

            if (project.OwnerId != userId)
            {
                var isAdmin = _http.HttpContext?.User?.IsInRole("Admin") ?? false;
                if (!isAdmin)
                    throw new UnauthorizedAccessException("You don't have permission to edit this workflow. Only project owners or admins can modify workflow configurations.");
            }

            if (request.States.Count == 0)
                throw new InvalidOperationException("Workflow must have at least one state.");

            Workflow workflow;

            if (project.WorkflowId.HasValue)
            {
                // Determine whether this workflow is shared (used by other projects
                // or referenced as a domain-template default). Shared workflows must
                // never be mutated — create a project-specific copy instead.
                var isShared = await IsWorkflowSharedAsync(
                    project.WorkflowId.Value, project.Id, cancellationToken);

                if (isShared)
                {
                    workflow = CreateNewWorkflow(request.Name, project.DomainType, userId);
                    _db.Workflows.Add(workflow);
                    project.WorkflowId = workflow.Id;
                }
                else
                {
                    // Exclusively owned by this project — safe to update in place.
                    workflow = await _db.Workflows
                        .Include(w => w.States)
                        .FirstOrDefaultAsync(w => w.Id == project.WorkflowId.Value, cancellationToken)
                        ?? throw new InvalidOperationException("Existing project workflow not found");

                    workflow.Name = request.Name;
                    workflow.UpdatedAt = DateTime.UtcNow;

                    // Remove old states that are not referenced by transitions.
                    // For states with transition references, soft-deactivate instead.
                    var oldStateIds = workflow.States.Select(s => s.Id).ToList();
                    var referencedStateIds = oldStateIds.Count > 0
                        ? await _db.WorkflowTransitions
                            .Where(wt => oldStateIds.Contains(wt.FromStateId) || oldStateIds.Contains(wt.ToStateId))
                            .Select(wt => wt.FromStateId)
                            .Union(_db.WorkflowTransitions
                                .Where(wt => oldStateIds.Contains(wt.FromStateId) || oldStateIds.Contains(wt.ToStateId))
                                .Select(wt => wt.ToStateId))
                            .Distinct()
                            .ToListAsync(cancellationToken)
                        : new List<Guid>();

                    var safeToDrop = workflow.States
                        .Where(s => !referencedStateIds.Contains(s.Id))
                        .ToList();
                    var mustKeep = workflow.States
                        .Where(s => referencedStateIds.Contains(s.Id))
                        .ToList();

                    _db.WorkflowStates.RemoveRange(safeToDrop);

                    // Deactivate states that are referenced by history but no longer
                    // part of the new workflow definition.
                    foreach (var kept in mustKeep)
                    {
                        kept.IsActive = false;
                        kept.UpdatedAt = DateTime.UtcNow;
                    }

                    workflow.States.Clear();
                }
            }
            else
            {
                workflow = CreateNewWorkflow(request.Name, project.DomainType, userId);
                _db.Workflows.Add(workflow);
                project.WorkflowId = workflow.Id;
            }

            // First pass: create states and build name→Id map
            var stateMap = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
            foreach (var dto in request.States)
            {
                var state = new WorkflowState
                {
                    Id = Guid.NewGuid(),
                    WorkflowId = workflow.Id,
                    Name = dto.Name,
                    Order = dto.Order,
                    Color = dto.Color,
                    IsInitial = dto.IsInitial,
                    IsFinal = dto.IsFinal,
                    NotifyOnEntry = dto.NotifyOnEntry,
                    RequiredFields = dto.RequiredFields is { Count: > 0 }
                        ? JsonSerializer.Serialize(dto.RequiredFields) : null,
                    CreatedBy = userId.ToString()
                };
                stateMap[dto.Name] = state.Id;
                workflow.States.Add(state);
            }

            // Second pass: resolve allowed transitions by name → GUID
            for (int i = 0; i < request.States.Count; i++)
            {
                var dto = request.States[i];
                if (dto.AllowedTransitionNames is { Count: > 0 })
                {
                    var ids = dto.AllowedTransitionNames
                        .Where(n => stateMap.ContainsKey(n))
                        .Select(n => stateMap[n])
                        .ToList();
                    workflow.States[i].AllowedTransitions = ids.Count > 0
                        ? JsonSerializer.Serialize(ids) : null;
                }
            }

            project.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return workflow.Id;
        }

        /// <summary>
        /// A workflow is shared if any other project references it or if it is
        /// used as a domain-template default.
        /// </summary>
        private async Task<bool> IsWorkflowSharedAsync(
            Guid workflowId, Guid currentProjectId, CancellationToken cancellationToken)
        {
            var usedByOtherProject = await _db.Projects
                .AnyAsync(p => p.WorkflowId == workflowId && p.Id != currentProjectId, cancellationToken);
            if (usedByOtherProject)
                return true;

            var usedByTemplate = await _db.DomainTemplates
                .AnyAsync(dt => dt.DefaultWorkflowId == workflowId, cancellationToken);
            return usedByTemplate;
        }

        private static Workflow CreateNewWorkflow(string name, Domain.Enums.DomainType domainType, Guid userId)
        {
            return new Workflow
            {
                Id = Guid.NewGuid(),
                Name = name,
                DomainType = domainType,
                CreatedBy = userId.ToString()
            };
        }
    }
}
