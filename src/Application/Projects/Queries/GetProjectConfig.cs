using Application.Common.Interfaces;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Projects.Queries
{
    public record GetProjectConfigQuery(Guid ProjectId) : IRequest<ProjectConfigDto>;

    public record ProjectConfigDto(
        Guid ProjectId,
        string DomainType,
        Dictionary<string, string> WorkItemTypeLabels);

    public class GetProjectConfigHandler : IRequestHandler<GetProjectConfigQuery, ProjectConfigDto>
    {
        private readonly IAppDbContext _db;

        public GetProjectConfigHandler(IAppDbContext db)
        {
            _db = db;
        }

        public async Task<ProjectConfigDto> Handle(GetProjectConfigQuery request, CancellationToken cancellationToken)
        {
            var project = await _db.Projects
                .AsNoTracking()
                .Include(p => p.Template)
                .FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken);

            if (project is null)
                throw new InvalidOperationException("Project not found");

            // 1) Try the template's persisted labels first
            Dictionary<string, string>? labels = null;
            if (project.Template?.WorkItemTypeLabels is not null)
            {
                try
                {
                    var parsed = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(
                        project.Template.WorkItemTypeLabels);
                    if (parsed is not null && parsed.Count > 0)
                        labels = parsed;
                }
                catch { /* ignore parse errors */ }
            }

            // 2) Fall back to the built-in domain-type map (always works even if
            //    the template column hasn't been backfilled yet)
            labels ??= GetLabelsByDomainType(project.DomainType);

            return new ProjectConfigDto(
                project.Id,
                project.DomainType.ToString(),
                labels);
        }

        /// <summary>
        /// Authoritative mapping of DomainType → work-item hierarchy labels.
        /// This is the single source of truth that never depends on DB state.
        /// </summary>
        internal static Dictionary<string, string> GetLabelsByDomainType(DomainType domainType)
        {
            return domainType switch
            {
                DomainType.IT => new() { ["1"] = "Epic", ["2"] = "User Story", ["3"] = "Task", ["4"] = "SubTask" },
                DomainType.Healthcare => new() { ["1"] = "Initiative", ["2"] = "Action Item", ["3"] = "Task", ["4"] = "SubTask" },
                DomainType.PublicSafety => new() { ["1"] = "Operation", ["2"] = "Action Plan", ["3"] = "Task", ["4"] = "SubTask" },
                DomainType.Construction => new() { ["1"] = "Phase", ["2"] = "Activity", ["3"] = "Punch Item", ["4"] = "SubItem" },
                DomainType.Infrastructure => new() { ["1"] = "Program", ["2"] = "Work Package", ["3"] = "Task", ["4"] = "SubTask" },
                DomainType.EconomicDevelopment => new() { ["1"] = "Program", ["2"] = "Initiative", ["3"] = "Task", ["4"] = "SubTask" },
                DomainType.Technology => new() { ["1"] = "Epic", ["2"] = "Feature", ["3"] = "Task", ["4"] = "SubTask" },
                _ => new() { ["1"] = "Epic", ["2"] = "User Story", ["3"] = "Task", ["4"] = "SubTask" }
            };
        }
    }
}
