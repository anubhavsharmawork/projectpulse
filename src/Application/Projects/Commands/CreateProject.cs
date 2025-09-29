using Application.Common.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Application.Projects.Commands
{
    // Command + Result records used by API and handler
    public record CreateProjectCommand(string Name, string? Description) : IRequest<CreateProjectResult>;
    public record CreateProjectResult(Guid ProjectId);

    public class CreateProjectHandler : IRequestHandler<CreateProjectCommand, CreateProjectResult>
    {
        private readonly IAppDbContext _db;
        private readonly IHttpContextAccessor _http;
        public CreateProjectHandler(IAppDbContext db, IHttpContextAccessor http)
        {
            _db = db; _http = http;
        }

        public async Task<CreateProjectResult> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
        {
            var ownerIdClaim = _http.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var ownerId = Guid.TryParse(ownerIdClaim, out var id) ? id : Guid.Empty;

            var entity = new Domain.Entities.Project
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Description = request.Description,
                OwnerId = ownerId
            };
            _db.Projects.Add(entity);
            await _db.SaveChangesAsync(cancellationToken);
            return new CreateProjectResult(entity.Id);
        }
    }
}
