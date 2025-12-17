using Application.Common.Interfaces;
using Domain.Entities;
using MediatR;

namespace Application.WorkItems.Commands
{
    public record CreateUserStoryCommand(Guid ProjectId, string Title, string? Description, string? AttachmentUrl = null, Guid? ParentId = null) : IRequest<CreateUserStoryResult>;
    public record CreateUserStoryResult(Guid UserStoryId);

    public class CreateUserStoryHandler : IRequestHandler<CreateUserStoryCommand, CreateUserStoryResult>
    {
        private readonly IAppDbContext _db;
        public CreateUserStoryHandler(IAppDbContext db) => _db = db;

        public async Task<CreateUserStoryResult> Handle(CreateUserStoryCommand request, CancellationToken cancellationToken)
        {
            var entity = new UserStoryWorkItem
            {
                Id = Guid.NewGuid(),
                ProjectId = request.ProjectId,
                ParentId = request.ParentId,
                Title = request.Title,
                Description = request.Description,
                AttachmentUrl = request.AttachmentUrl,
                IsCompleted = false
            };
            _db.WorkItems.Add(entity);
            await _db.SaveChangesAsync(cancellationToken);
            return new CreateUserStoryResult(entity.Id);
        }
    }
}
