using Application.Common.Interfaces;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Workflows.Queries
{
    public record GetProjectWorkflowQuery(Guid ProjectId) : IRequest<WorkflowDto?>;

    public class GetProjectWorkflowHandler : IRequestHandler<GetProjectWorkflowQuery, WorkflowDto?>
    {
        private readonly IAppDbContext _db;

        public GetProjectWorkflowHandler(IAppDbContext db) => _db = db;

        public async Task<WorkflowDto?> Handle(GetProjectWorkflowQuery request, CancellationToken cancellationToken)
        {
            var project = await _db.Projects
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken);

            if (project is null)
                return null;

            // If the project has a specific workflow override, use it
            if (project.WorkflowId.HasValue)
            {
                var projectWorkflow = await _db.Workflows
                    .AsNoTracking()
                    .Include(w => w.States.OrderBy(s => s.Order))
                    .FirstOrDefaultAsync(w => w.Id == project.WorkflowId.Value, cancellationToken);

                if (projectWorkflow is not null)
                    return MapToDto(projectWorkflow);
            }

            // Fall back to domain default
            var domainWorkflow = await _db.Workflows
                .AsNoTracking()
                .Include(w => w.States.OrderBy(s => s.Order))
                .FirstOrDefaultAsync(w => w.DomainType == project.DomainType, cancellationToken);

            return domainWorkflow is not null ? MapToDto(domainWorkflow) : null;
        }

        private static WorkflowDto MapToDto(Domain.Entities.Workflow workflow)
        {
            return new WorkflowDto(
                workflow.Id,
                workflow.Name,
                workflow.DomainType.ToString(),
                workflow.States.Select(s => new WorkflowStateDto(
                    s.Id,
                    s.Name,
                    s.Order,
                    s.Color,
                    s.IsInitial,
                    s.IsFinal,
                    ParseGuidList(s.AllowedTransitions),
                    ParseStringList(s.RequiredFields),
                    s.NotifyOnEntry
                )).ToList());
        }

        private static List<Guid> ParseGuidList(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new();
            try { return System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(json) ?? new(); }
            catch { return new(); }
        }

        private static List<string> ParseStringList(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new();
            try { return System.Text.Json.JsonSerializer.Deserialize<List<string>>(json) ?? new(); }
            catch { return new(); }
        }
    }
}
