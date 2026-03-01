using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Teams.Queries
{
    public record GetProjectRolesQuery(Guid ProjectId) : IRequest<List<ProjectRoleDto>>;

    public record ProjectRoleDto(Guid Id, string RoleName);

    public class GetProjectRolesHandler : IRequestHandler<GetProjectRolesQuery, List<ProjectRoleDto>>
    {
        private readonly IAppDbContext _db;

        public GetProjectRolesHandler(IAppDbContext db)
        {
            _db = db;
        }

        public async Task<List<ProjectRoleDto>> Handle(GetProjectRolesQuery request, CancellationToken cancellationToken)
        {
            var roles = await _db.ProjectRoles
                .AsNoTracking()
                .Where(pr => pr.ProjectId == request.ProjectId)
                .OrderBy(pr => pr.RoleName)
                .Select(pr => new ProjectRoleDto(pr.Id, pr.RoleName))
                .ToListAsync(cancellationToken);

            // If no project-specific roles exist yet, return sensible defaults
            if (roles.Count == 0)
            {
                var project = await _db.Projects
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken);

                if (project is null)
                    return new List<ProjectRoleDto>();

                var defaultRoles = GetDefaultRolesForDomain(project.DomainType);
                return defaultRoles.Select(r => new ProjectRoleDto(Guid.Empty, r)).ToList();
            }

            return roles;
        }

        private static List<string> GetDefaultRolesForDomain(Domain.Enums.DomainType domainType)
        {
            return domainType switch
            {
                Domain.Enums.DomainType.Construction => new List<string>
                {
                    "Project Manager", "Site Supervisor", "Safety Officer",
                    "Civil Engineer", "Architect", "Foreman", "Inspector", "Estimator"
                },
                Domain.Enums.DomainType.Healthcare => new List<string>
                {
                    "Project Manager", "Clinical Lead", "Compliance Officer",
                    "IT Specialist", "Business Analyst", "QA Analyst", "Trainer"
                },
                Domain.Enums.DomainType.IT => new List<string>
                {
                    "Developer", "QA", "Designer", "Project Manager",
                    "Tech Lead", "Business Analyst", "DevOps"
                },
                Domain.Enums.DomainType.Technology => new List<string>
                {
                    "Developer", "QA Engineer", "UX Designer", "Product Manager",
                    "Tech Lead", "Data Engineer", "DevOps Engineer"
                },
                Domain.Enums.DomainType.Infrastructure => new List<string>
                {
                    "Project Manager", "Systems Engineer", "Network Engineer",
                    "Field Technician", "Safety Coordinator", "Environmental Analyst"
                },
                Domain.Enums.DomainType.EconomicDevelopment => new List<string>
                {
                    "Project Manager", "Policy Analyst", "Community Liaison",
                    "Grant Writer", "Data Analyst", "Compliance Officer"
                },
                Domain.Enums.DomainType.PublicSafety => new List<string>
                {
                    "Project Manager", "Operations Lead", "Training Coordinator",
                    "Communications Officer", "IT Specialist", "Field Coordinator"
                },
                _ => new List<string>
                {
                    "Developer", "QA", "Designer", "Project Manager",
                    "Tech Lead", "Business Analyst", "DevOps"
                }
            };
        }
    }
}
