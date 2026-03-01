using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Teams.Commands
{
    public record UpdateTeamMemberCommand(
        Guid TeamMemberId,
        string Role,
        string? DomainExpertise,
        string? Skills,
        decimal AvailabilityHoursPerWeek,
        decimal CostRate) : IRequest<Unit>;

    public class UpdateTeamMemberHandler : IRequestHandler<UpdateTeamMemberCommand, Unit>
    {
        private readonly IAppDbContext _db;

        public UpdateTeamMemberHandler(IAppDbContext db)
        {
            _db = db;
        }

        public async Task<Unit> Handle(UpdateTeamMemberCommand request, CancellationToken cancellationToken)
        {
            var member = await _db.TeamMembers.FirstOrDefaultAsync(
                tm => tm.Id == request.TeamMemberId, cancellationToken);

            if (member is null)
                throw new InvalidOperationException("Team member not found");

            member.Role = request.Role;
            member.DomainExpertise = request.DomainExpertise;
            member.Skills = request.Skills;
            member.AvailabilityHoursPerWeek = request.AvailabilityHoursPerWeek;
            member.CostRate = request.CostRate;
            member.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);
            return Unit.Value;
        }
    }
}
