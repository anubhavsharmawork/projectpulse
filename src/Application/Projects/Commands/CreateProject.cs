using Application.Common.Interfaces;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Application.Projects.Commands
{
    // Command + Result records used by API and handler
    public record CreateProjectCommand(
        string Name,
        string? Description,
        bool IsPublic = false,
        DomainType? DomainType = null,
        decimal EstimatedCost = 0) : IRequest<CreateProjectResult>;
    public record CreateProjectResult(Guid ProjectId);

    public class CreateProjectHandler : IRequestHandler<CreateProjectCommand, CreateProjectResult>
    {
        private readonly IAppDbContext _db;
        private readonly IHttpContextAccessor _http;
        public CreateProjectHandler(IAppDbContext db, IHttpContextAccessor http)
        {
            _db = db; _http = http;
        }

        public async Task<CreateProjectResult> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
        {
            var ownerIdClaim = _http.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var ownerId = Guid.TryParse(ownerIdClaim, out var id) ? id : Guid.Empty;

            var entity = new Domain.Entities.Project
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Description = request.Description,
                OwnerId = ownerId,
                IsPublic = request.IsPublic,
                EstimatedCost = request.EstimatedCost
            };

            // If a domain type is specified, link the matching workflow and template
            if (request.DomainType.HasValue)
            {
                entity.DomainType = request.DomainType.Value;

                var workflow = await _db.Workflows
                    .AsNoTracking()
                    .FirstOrDefaultAsync(w => w.DomainType == request.DomainType.Value, cancellationToken);
                if (workflow is not null)
                    entity.WorkflowId = workflow.Id;

                var template = await _db.DomainTemplates
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.DomainType == request.DomainType.Value, cancellationToken);
                if (template is not null)
                    entity.TemplateId = template.Id;
            }

            _db.Projects.Add(entity);

            // Seed domain-specific project roles when a domain type is specified
            if (request.DomainType.HasValue)
            {
                var defaultRoles = GetDefaultRolesForDomain(request.DomainType.Value);
                foreach (var roleName in defaultRoles)
                {
                    _db.ProjectRoles.Add(new Domain.Entities.ProjectRole
                    {
                        Id = Guid.NewGuid(),
                        ProjectId = entity.Id,
                        RoleName = roleName,
                        DomainType = request.DomainType.Value
                    });
                }
            }

            await _db.SaveChangesAsync(cancellationToken);
            return new CreateProjectResult(entity.Id);
        }

        private static List<string> GetDefaultRolesForDomain(DomainType domainType)
        {
            return domainType switch
            {
                DomainType.Construction => new List<string>
                {
                    "Project Manager", "Site Supervisor", "Safety Officer",
                    "Civil Engineer", "Architect", "Foreman", "Inspector", "Estimator"
                },
                DomainType.Healthcare => new List<string>
                {
                    "Project Manager", "Clinical Lead", "Compliance Officer",
                    "IT Specialist", "Business Analyst", "QA Analyst", "Trainer"
                },
                DomainType.IT => new List<string>
                {
                    "Developer", "QA", "Designer", "Project Manager",
                    "Tech Lead", "Business Analyst", "DevOps"
                },
                DomainType.Technology => new List<string>
                {
                    "Developer", "QA Engineer", "UX Designer", "Product Manager",
                    "Tech Lead", "Data Engineer", "DevOps Engineer"
                },
                DomainType.Infrastructure => new List<string>
                {
                    "Project Manager", "Systems Engineer", "Network Engineer",
                    "Field Technician", "Safety Coordinator", "Environmental Analyst"
                },
                DomainType.EconomicDevelopment => new List<string>
                {
                    "Project Manager", "Policy Analyst", "Community Liaison",
                    "Grant Writer", "Data Analyst", "Compliance Officer"
                },
                DomainType.PublicSafety => new List<string>
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
