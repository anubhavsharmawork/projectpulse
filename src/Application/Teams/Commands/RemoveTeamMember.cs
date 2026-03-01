using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Teams.Commands
{
    public record RemoveTeamMemberCommand(Guid TeamMemberId) : IRequest<Unit>;

    public class RemoveTeamMemberHandler : IRequestHandler<RemoveTeamMemberCommand, Unit>
    {
        private readonly IAppDbContext _db;

        public RemoveTeamMemberHandler(IAppDbContext db)
        {
            _db = db;
        }

        public async Task<Unit> Handle(RemoveTeamMemberCommand request, CancellationToken cancellationToken)
        {
            var member = await _db.TeamMembers.FirstOrDefaultAsync(
                tm => tm.Id == request.TeamMemberId, cancellationToken);

            if (member is null)
                throw new InvalidOperationException("Team member not found");

            _db.TeamMembers.Remove(member);
            await _db.SaveChangesAsync(cancellationToken);
            return Unit.Value;
        }
    }
}
