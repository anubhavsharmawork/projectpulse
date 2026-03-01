using Application.Common.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Application.Teams.Commands
{
    public record CreateTeamMemberCommand(
        Guid TeamId,
        Guid UserId,
        string Role,
        string? DomainExpertise,
        string? Skills,
        decimal AvailabilityHoursPerWeek,
        decimal CostRate) : IRequest<CreateTeamMemberResult>;

    public record CreateTeamMemberResult(Guid TeamMemberId);

    public class CreateTeamMemberHandler : IRequestHandler<CreateTeamMemberCommand, CreateTeamMemberResult>
    {
        private readonly IAppDbContext _db;
        private readonly IHttpContextAccessor _http;

        public CreateTeamMemberHandler(IAppDbContext db, IHttpContextAccessor http)
        {
            _db = db;
            _http = http;
        }

        public async Task<CreateTeamMemberResult> Handle(CreateTeamMemberCommand request, CancellationToken cancellationToken)
        {
            var team = await _db.Teams.FirstOrDefaultAsync(t => t.Id == request.TeamId, cancellationToken);
            if (team is null)
                throw new InvalidOperationException("Team not found");

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
            if (user is null)
                throw new InvalidOperationException("User not found");

            var exists = await _db.TeamMembers.AnyAsync(
                tm => tm.TeamId == request.TeamId && tm.UserId == request.UserId, cancellationToken);
            if (exists)
                throw new InvalidOperationException("User is already a member of this team");

            var createdBy = _http.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

            var entity = new Domain.Entities.TeamMember
            {
                Id = Guid.NewGuid(),
                TeamId = request.TeamId,
                UserId = request.UserId,
                Role = request.Role,
                DomainExpertise = request.DomainExpertise,
                Skills = request.Skills,
                AvailabilityHoursPerWeek = request.AvailabilityHoursPerWeek,
                CostRate = request.CostRate,
                CreatedBy = createdBy
            };

            _db.TeamMembers.Add(entity);
            await _db.SaveChangesAsync(cancellationToken);
            return new CreateTeamMemberResult(entity.Id);
        }
    }
}
