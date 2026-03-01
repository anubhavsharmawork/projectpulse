using Application.Common.Interfaces;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Workflows.Queries
{
    public record GetWorkflowDomainsQuery() : IRequest<List<WorkflowDomainDto>>;

    public record WorkflowDomainDto(string DomainType, bool HasDefault);

    public class GetWorkflowDomainsHandler : IRequestHandler<GetWorkflowDomainsQuery, List<WorkflowDomainDto>>
    {
        private readonly IAppDbContext _db;

        public GetWorkflowDomainsHandler(IAppDbContext db) => _db = db;

        public async Task<List<WorkflowDomainDto>> Handle(GetWorkflowDomainsQuery request, CancellationToken cancellationToken)
        {
            var domainsWithWorkflows = await _db.Workflows
                .AsNoTracking()
                .Select(w => w.DomainType)
                .Distinct()
                .ToListAsync(cancellationToken);

            var allDomains = Enum.GetValues<DomainType>();
            return allDomains.Select(d => new WorkflowDomainDto(
                d.ToString(),
                domainsWithWorkflows.Contains(d)
            )).ToList();
        }
    }
}
