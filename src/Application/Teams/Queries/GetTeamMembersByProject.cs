using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Teams.Queries
{
    public record GetTeamMembersByProjectQuery(Guid ProjectId) : IRequest<List<TeamMemberDto>>;

    public class GetTeamMembersByProjectHandler : IRequestHandler<GetTeamMembersByProjectQuery, List<TeamMemberDto>>
    {
        private readonly IAppDbContext _db;

        public GetTeamMembersByProjectHandler(IAppDbContext db)
        {
            _db = db;
        }

        public async Task<List<TeamMemberDto>> Handle(GetTeamMembersByProjectQuery request, CancellationToken cancellationToken)
        {
            var teamIds = await _db.Teams
                .AsNoTracking()
                .Where(t => t.ProjectId == request.ProjectId)
                .Select(t => t.Id)
                .ToListAsync(cancellationToken);

            if (teamIds.Count == 0)
                return new List<TeamMemberDto>();

            var members = await _db.TeamMembers
                .AsNoTracking()
                .Where(tm => teamIds.Contains(tm.TeamId) && tm.IsActive)
                .Include(tm => tm.User)
                .Select(tm => new TeamMemberDto(
                    tm.Id,
                    tm.UserId,
                    tm.User.DisplayName,
                    tm.User.Email,
                    tm.Role,
                    tm.DomainExpertise,
                    tm.Skills,
                    tm.AvailabilityHoursPerWeek,
                    tm.CostRate,
                    tm.IsActive,
                    tm.CreatedAt))
                .ToListAsync(cancellationToken);

            return members;
        }
    }
}
