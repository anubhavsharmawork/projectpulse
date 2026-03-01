using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Teams.Queries
{
    public record GetTeamMembersQuery(Guid TeamId) : IRequest<List<TeamMemberDto>>;

    public record TeamMemberDto(
        Guid Id,
        Guid UserId,
        string DisplayName,
        string Email,
        string Role,
        string? DomainExpertise,
        string? Skills,
        decimal AvailabilityHoursPerWeek,
        decimal CostRate,
        bool IsActive,
        DateTime CreatedAt);

    public class GetTeamMembersHandler : IRequestHandler<GetTeamMembersQuery, List<TeamMemberDto>>
    {
        private readonly IAppDbContext _db;

        public GetTeamMembersHandler(IAppDbContext db)
        {
            _db = db;
        }

        public async Task<List<TeamMemberDto>> Handle(GetTeamMembersQuery request, CancellationToken cancellationToken)
        {
            var members = await _db.TeamMembers
                .AsNoTracking()
                .Where(tm => tm.TeamId == request.TeamId && tm.IsActive)
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
