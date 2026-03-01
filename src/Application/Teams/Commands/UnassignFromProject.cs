using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Teams.Commands
{
    public record UnassignFromProjectCommand(Guid ProjectId, Guid UserId) : IRequest<Unit>;

    public class UnassignFromProjectHandler : IRequestHandler<UnassignFromProjectCommand, Unit>
    {
        private readonly IAppDbContext _db;

        public UnassignFromProjectHandler(IAppDbContext db)
        {
            _db = db;
        }

        public async Task<Unit> Handle(UnassignFromProjectCommand request, CancellationToken cancellationToken)
        {
            var teamIds = await _db.Teams
                .Where(t => t.ProjectId == request.ProjectId)
                .Select(t => t.Id)
                .ToListAsync(cancellationToken);

            if (teamIds.Count == 0)
                throw new InvalidOperationException("No teams found for this project");

            var member = await _db.TeamMembers.FirstOrDefaultAsync(
                tm => teamIds.Contains(tm.TeamId) && tm.UserId == request.UserId, cancellationToken);

            if (member is null)
                throw new InvalidOperationException("User is not assigned to this project");

            _db.TeamMembers.Remove(member);
            await _db.SaveChangesAsync(cancellationToken);
            return Unit.Value;
        }
    }
}
