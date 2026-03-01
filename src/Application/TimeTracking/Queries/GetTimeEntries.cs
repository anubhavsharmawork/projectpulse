using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.TimeTracking.Queries
{
    public record GetTimeEntriesQuery(
        Guid? WorkItemId = null,
        Guid? UserId = null,
        Guid? ProjectId = null,
        DateTime? From = null,
        DateTime? To = null) : IRequest<List<TimeEntryDto>>;

    public record TimeEntryDto(
        Guid Id,
        Guid WorkItemId,
        string WorkItemTitle,
        Guid UserId,
        string UserDisplayName,
        decimal Hours,
        DateTime LoggedDate,
        string? Description,
        bool IsBillable);

    public class GetTimeEntriesHandler : IRequestHandler<GetTimeEntriesQuery, List<TimeEntryDto>>
    {
        private readonly IAppDbContext _db;

        public GetTimeEntriesHandler(IAppDbContext db) => _db = db;

        public async Task<List<TimeEntryDto>> Handle(GetTimeEntriesQuery request, CancellationToken cancellationToken)
        {
            var query = _db.TimeEntries
                .AsNoTracking()
                .Include(te => te.WorkItem)
                .Include(te => te.User)
                .AsQueryable();

            if (request.WorkItemId.HasValue)
                query = query.Where(te => te.WorkItemId == request.WorkItemId.Value);

            if (request.UserId.HasValue)
                query = query.Where(te => te.UserId == request.UserId.Value);

            if (request.ProjectId.HasValue)
                query = query.Where(te => te.WorkItem.ProjectId == request.ProjectId.Value);

            if (request.From.HasValue)
                query = query.Where(te => te.LoggedDate >= request.From.Value);

            if (request.To.HasValue)
                query = query.Where(te => te.LoggedDate <= request.To.Value);

            return await query
                .OrderByDescending(te => te.LoggedDate)
                .Select(te => new TimeEntryDto(
                    te.Id,
                    te.WorkItemId,
                    te.WorkItem.Title,
                    te.UserId,
                    te.User.DisplayName,
                    te.Hours,
                    te.LoggedDate,
                    te.Description,
                    te.IsBillable))
                .ToListAsync(cancellationToken);
        }
    }
}
