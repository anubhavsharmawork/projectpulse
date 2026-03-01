using Application.Common.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Application.TimeTracking.Commands
{
    public record LogTimeEntryCommand(
        Guid WorkItemId,
        decimal Hours,
        DateTime LoggedDate,
        string? Description,
        bool IsBillable) : IRequest<Guid>;

    public class LogTimeEntryHandler : IRequestHandler<LogTimeEntryCommand, Guid>
    {
        private readonly IAppDbContext _db;
        private readonly IHttpContextAccessor _http;

        public LogTimeEntryHandler(IAppDbContext db, IHttpContextAccessor http)
        {
            _db = db;
            _http = http;
        }

        public async Task<Guid> Handle(LogTimeEntryCommand request, CancellationToken cancellationToken)
        {
            var userIdClaim = _http.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userId = Guid.TryParse(userIdClaim, out var id) ? id : Guid.Empty;

            var workItem = await _db.WorkItems
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.Id == request.WorkItemId, cancellationToken);

            if (workItem is null)
                throw new InvalidOperationException("Work item not found");

            var project = await _db.Projects
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == workItem.ProjectId, cancellationToken);

            if (project is null)
                throw new InvalidOperationException("Project not found for this work item");

            if (!project.IsPublic && project.OwnerId != userId)
                throw new UnauthorizedAccessException("You don't have access to this work item.");

            var entry = new Domain.Entities.TimeEntry
            {
                Id = Guid.NewGuid(),
                WorkItemId = request.WorkItemId,
                UserId = userId,
                Hours = request.Hours,
                LoggedDate = request.LoggedDate,
                Description = request.Description,
                IsBillable = request.IsBillable,
                CreatedBy = userId.ToString()
            };

            _db.TimeEntries.Add(entry);
            await _db.SaveChangesAsync(cancellationToken);
            return entry.Id;
        }
    }
}
