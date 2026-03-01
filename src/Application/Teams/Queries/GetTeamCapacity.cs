using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Teams.Queries
{
    public record GetTeamCapacityQuery(Guid TeamId) : IRequest<TeamCapacityDto>;

    public record TeamCapacityDto(
        Guid TeamId,
        string TeamName,
        int TotalMembers,
        decimal TotalAvailableHours,
        decimal TotalAllocatedHours,
        decimal UtilizationPercentage,
        List<MemberCapacityDto> Members);

    public record MemberCapacityDto(
        Guid TeamMemberId,
        Guid UserId,
        string DisplayName,
        string Role,
        decimal AvailableHoursPerWeek,
        decimal AllocatedHours,
        decimal UtilizationPercentage,
        int AssignedTaskCount,
        decimal CostRate);

    public class GetTeamCapacityHandler : IRequestHandler<GetTeamCapacityQuery, TeamCapacityDto>
    {
        private readonly IAppDbContext _db;

        public GetTeamCapacityHandler(IAppDbContext db)
        {
            _db = db;
        }

        public async Task<TeamCapacityDto> Handle(GetTeamCapacityQuery request, CancellationToken cancellationToken)
        {
            var team = await _db.Teams
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == request.TeamId, cancellationToken);

            if (team is null)
                throw new InvalidOperationException("Team not found");

            var members = await _db.TeamMembers
                .AsNoTracking()
                .Where(tm => tm.TeamId == request.TeamId && tm.IsActive)
                .Include(tm => tm.User)
                .ToListAsync(cancellationToken);

            var memberUserIds = members.Select(m => m.UserId).ToList();

            // Get assigned (non-completed) work item counts per user
            var taskCounts = await _db.WorkItems
                .AsNoTracking()
                .Where(wi => wi.ProjectId == team.ProjectId
                             && wi.AssigneeId.HasValue
                             && memberUserIds.Contains(wi.AssigneeId.Value)
                             && !wi.IsCompleted)
                .GroupBy(wi => wi.AssigneeId!.Value)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            // Get logged hours per user (last 7 days as proxy for current sprint)
            var weekAgo = DateTime.UtcNow.AddDays(-7);
            var loggedHours = await _db.TimeEntries
                .AsNoTracking()
                .Where(te => memberUserIds.Contains(te.UserId)
                             && te.LoggedDate >= weekAgo)
                .GroupBy(te => te.UserId)
                .Select(g => new { UserId = g.Key, Hours = g.Sum(te => te.Hours) })
                .ToListAsync(cancellationToken);

            var memberCapacities = members.Select(m =>
            {
                var tasks = taskCounts.FirstOrDefault(tc => tc.UserId == m.UserId)?.Count ?? 0;
                var allocated = loggedHours.FirstOrDefault(lh => lh.UserId == m.UserId)?.Hours ?? 0m;
                var utilization = m.AvailabilityHoursPerWeek > 0
                    ? Math.Round(allocated / m.AvailabilityHoursPerWeek * 100, 1)
                    : 0m;

                return new MemberCapacityDto(
                    m.Id,
                    m.UserId,
                    m.User.DisplayName,
                    m.Role,
                    m.AvailabilityHoursPerWeek,
                    allocated,
                    utilization,
                    tasks,
                    m.CostRate);
            }).ToList();

            var totalAvailable = members.Sum(m => m.AvailabilityHoursPerWeek);
            var totalAllocated = memberCapacities.Sum(mc => mc.AllocatedHours);
            var totalUtilization = totalAvailable > 0
                ? Math.Round(totalAllocated / totalAvailable * 100, 1)
                : 0m;

            return new TeamCapacityDto(
                team.Id,
                team.Name,
                members.Count,
                totalAvailable,
                totalAllocated,
                totalUtilization,
                memberCapacities);
        }
    }
}
