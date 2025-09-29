using Application.Common.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Application.Comments.Commands
{
    public record CreateCommentCommand(Guid TaskId, string Body) : IRequest<CreateCommentResult>;
    public record CreateCommentResult(Guid CommentId);

    public class CreateCommentHandler : IRequestHandler<CreateCommentCommand, CreateCommentResult>
    {
        private readonly IAppDbContext _db;
        private readonly IHttpContextAccessor _http;
        public CreateCommentHandler(IAppDbContext db, IHttpContextAccessor http)
        {
            _db = db; _http = http;
        }

        public async Task<CreateCommentResult> Handle(CreateCommentCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Body))
                throw new ArgumentException("Comment body is required", nameof(request.Body));

            var userIdClaim = _http.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var authorId = Guid.TryParse(userIdClaim, out var id) ? id : Guid.Empty;

            var entity = new Domain.Entities.Comment
            {
                Id = Guid.NewGuid(),
                TaskItemId = request.TaskId,
                AuthorId = authorId,
                Body = request.Body.Trim()
            };
            _db.Comments.Add(entity);
            await _db.SaveChangesAsync(cancellationToken);
            return new CreateCommentResult(entity.Id);
        }
    }
}
