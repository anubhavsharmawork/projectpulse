using Application.Common.Interfaces;
using MediatR;

namespace Application.Tasks.Commands
{
    // Command + Result records used by API and handler
    public record CreateTaskCommand(Guid ProjectId, string Title, string? Description, string? AttachmentUrl = null) : IRequest<CreateTaskResult>;
    public record CreateTaskResult(Guid TaskId);

    public class CreateTaskHandler : IRequestHandler<CreateTaskCommand, CreateTaskResult>
    {
        private readonly IAppDbContext _db;
        public CreateTaskHandler(IAppDbContext db) => _db = db;

        public async Task<CreateTaskResult> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
        {
            var entity = new Domain.Entities.TaskItem
            {
                Id = Guid.NewGuid(),
                ProjectId = request.ProjectId,
                Title = request.Title,
                Description = request.Description,
                AttachmentUrl = request.AttachmentUrl,
                IsCompleted = false
            };
            _db.Tasks.Add(entity);
            await _db.SaveChangesAsync(cancellationToken);
            return new CreateTaskResult(entity.Id);
        }
    }
}
