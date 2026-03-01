using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Budget.Queries
{
    public record GetBudgetStatusQuery() : IRequest<List<ProjectBudgetDto>>;

    public record ProjectBudgetDto(
        Guid ProjectId,
        string ProjectName,
        string DomainType,
        decimal EstimatedCost,
        decimal ActualCost,
        decimal BudgetVariance,
        decimal VariancePercent,
        int EpicCount,
        decimal EpicEstimatedTotal,
        decimal EpicActualTotal);

    public class GetBudgetStatusHandler : IRequestHandler<GetBudgetStatusQuery, List<ProjectBudgetDto>>
    {
        private readonly IAppDbContext _db;

        public GetBudgetStatusHandler(IAppDbContext db) => _db = db;

        public async Task<List<ProjectBudgetDto>> Handle(GetBudgetStatusQuery request, CancellationToken cancellationToken)
        {
            var projects = await _db.Projects
                .AsNoTracking()
                .Where(p => p.IsActive)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    DomainType = p.DomainType.ToString(),
                    p.EstimatedCost,
                    p.ActualCost,
                    Epics = p.WorkItems
                        .Where(w => w.Type == Domain.Entities.WorkItemType.Epic)
                        .Select(e => new { e.EstimatedCost, e.ActualCost })
                        .ToList()
                })
                .ToListAsync(cancellationToken);

            return projects.Select(p =>
            {
                var variance = p.ActualCost - p.EstimatedCost;
                var variancePct = p.EstimatedCost > 0
                    ? Math.Round(variance / p.EstimatedCost * 100, 2)
                    : 0m;
                return new ProjectBudgetDto(
                    p.Id, p.Name, p.DomainType,
                    p.EstimatedCost, p.ActualCost, variance, variancePct,
                    p.Epics.Count,
                    p.Epics.Sum(e => e.EstimatedCost),
                    p.Epics.Sum(e => e.ActualCost));
            }).ToList();
        }
    }
}
