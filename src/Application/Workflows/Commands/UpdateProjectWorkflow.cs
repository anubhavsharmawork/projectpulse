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
                    // Load the workflow without eager-loading states to avoid keeping
                    // a set of tracked state entities that we might otherwise delete
                    // or mutate with ExecuteUpdate/ExecuteDelete operations below.
                    workflow = await _db.Workflows
                        .FirstOrDefaultAsync(w => w.Id == project.WorkflowId.Value, cancellationToken)
                        ?? throw new InvalidOperationException("Existing project workflow not found");

                    workflow.Name = request.Name;
                    workflow.UpdatedAt = DateTime.UtcNow;

                    // Determine old state ids for this workflow
                    var oldStateIds = await _db.WorkflowStates
                        .Where(s => s.WorkflowId == workflow.Id)
                        .Select(s => s.Id)
                        .ToListAsync(cancellationToken);

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

                    var safeToDropIds = oldStateIds.Except(referencedStateIds).ToList();
                    var mustKeepIds = oldStateIds.Intersect(referencedStateIds).ToList();

                    // Delete states that are safe to drop (no transitions reference them).
                    if (safeToDropIds.Count > 0)
                    {
                        var toDelete = await _db.WorkflowStates
                            .Where(s => safeToDropIds.Contains(s.Id))
                            .ToListAsync(cancellationToken);
                        if (toDelete.Count > 0)
                            _db.WorkflowStates.RemoveRange(toDelete);
                    }

                    // Soft-deactivate states that are referenced by transitions so
                    // history remains consistent. Use tracked entities to update
                    // fields so the InMemory provider and change tracker remain
                    // consistent.
                    if (mustKeepIds.Count > 0)
                    {
                        var toUpdate = await _db.WorkflowStates
                            .Where(s => mustKeepIds.Contains(s.Id))
                            .ToListAsync(cancellationToken);
                        foreach (var s in toUpdate)
                        {
                            s.IsActive = false;
                            s.UpdatedAt = DateTime.UtcNow;
                        }
                    }
                }
            }
            else
            {
                workflow = CreateNewWorkflow(request.Name, project.DomainType, userId);
                _db.Workflows.Add(workflow);
                project.WorkflowId = workflow.Id;
            }

            // First pass: create states and build name→Id map. Collect new states in a
            // separate list and add them directly to the DbSet to avoid manipulating
            // the Workflow.States navigation collection which can create tracking
            // conflicts with existing state/transition entities in the change tracker.
            var stateMap = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
            var newStates = new List<WorkflowState>();
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
                newStates.Add(state);
            }

            // Second pass: resolve allowed transitions by name → GUID and set on
            // the in-memory newStates list.
            for (int i = 0; i < request.States.Count; i++)
            {
                var dto = request.States[i];
                if (dto.AllowedTransitionNames is { Count: > 0 })
                {
                    var ids = dto.AllowedTransitionNames
                        .Where(n => stateMap.ContainsKey(n))
                        .Select(n => stateMap[n])
                        .ToList();
                    newStates[i].AllowedTransitions = ids.Count > 0
                        ? JsonSerializer.Serialize(ids) : null;
                }
            }

            // Attach the new states to the DbContext for insertion.
            if (newStates.Count > 0)
                _db.WorkflowStates.AddRange(newStates);

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
