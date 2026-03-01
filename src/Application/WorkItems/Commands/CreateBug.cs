using Application.Common.Interfaces;
using Domain.Entities;
using MediatR;

namespace Application.WorkItems.Commands
{
    public record CreateBugCommand(
        Guid ProjectId,
        string Title,
        string? Description = null,
        Guid? ParentId = null,
        BugSeverity Severity = BugSeverity.Medium,
        string? StepsToReproduce = null,
        string? ExpectedBehavior = null,
        string? ActualBehavior = null,
        string? Environment = null) : IRequest<CreateBugResult>;

    public record CreateBugResult(Guid BugId);

    public class CreateBugHandler : IRequestHandler<CreateBugCommand, CreateBugResult>
    {
        private readonly IAppDbContext _db;
        public CreateBugHandler(IAppDbContext db) => _db = db;

        public async Task<CreateBugResult> Handle(CreateBugCommand request, CancellationToken cancellationToken)
        {
            var entity = new BugWorkItem
            {
                Id = Guid.NewGuid(),
                ProjectId = request.ProjectId,
                ParentId = request.ParentId,
                Title = request.Title,
                Description = request.Description,
                Severity = request.Severity,
                StepsToReproduce = request.StepsToReproduce,
                ExpectedBehavior = request.ExpectedBehavior,
                ActualBehavior = request.ActualBehavior,
                Environment = request.Environment,
                IsCompleted = false
            };
            _db.WorkItems.Add(entity);
            await _db.SaveChangesAsync(cancellationToken);
            return new CreateBugResult(entity.Id);
        }
    }
}
