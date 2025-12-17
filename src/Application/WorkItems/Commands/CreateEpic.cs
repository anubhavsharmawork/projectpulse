using Application.Common.Interfaces;
using Domain.Entities;
using MediatR;

namespace Application.WorkItems.Commands
{
    public record CreateEpicCommand(Guid ProjectId, string Title, string? Description, string? AttachmentUrl = null) : IRequest<CreateEpicResult>;
    public record CreateEpicResult(Guid EpicId);

    public class CreateEpicHandler : IRequestHandler<CreateEpicCommand, CreateEpicResult>
    {
        private readonly IAppDbContext _db;
        public CreateEpicHandler(IAppDbContext db) => _db = db;

        public async Task<CreateEpicResult> Handle(CreateEpicCommand request, CancellationToken cancellationToken)
        {
            var entity = new EpicWorkItem
            {
                Id = Guid.NewGuid(),
                ProjectId = request.ProjectId,
                Title = request.Title,
                Description = request.Description,
                AttachmentUrl = request.AttachmentUrl,
                IsCompleted = false
            };
            _db.WorkItems.Add(entity);
            await _db.SaveChangesAsync(cancellationToken);
            return new CreateEpicResult(entity.Id);
        }
    }
}
