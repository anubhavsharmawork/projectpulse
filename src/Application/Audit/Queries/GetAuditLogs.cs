using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Audit.Queries
{
    public record GetAuditLogsQuery(
        string? EntityType = null,
        Guid? EntityId = null,
        Guid? UserId = null,
        DateTime? From = null,
        DateTime? To = null,
        int Limit = 100) : IRequest<List<AuditLogDto>>;

    public record AuditLogDto(
        Guid Id,
        string EntityType,
        Guid EntityId,
        string Action,
        string? OldValues,
        string? NewValues,
        Guid? UserId,
        DateTime Timestamp);

    public class GetAuditLogsHandler : IRequestHandler<GetAuditLogsQuery, List<AuditLogDto>>
    {
        private readonly IAppDbContext _db;

        public GetAuditLogsHandler(IAppDbContext db) => _db = db;

        public async Task<List<AuditLogDto>> Handle(GetAuditLogsQuery request, CancellationToken cancellationToken)
        {
            var query = _db.AuditLogs.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.EntityType))
                query = query.Where(a => a.EntityType == request.EntityType);

            if (request.EntityId.HasValue)
                query = query.Where(a => a.EntityId == request.EntityId.Value);

            if (request.UserId.HasValue)
                query = query.Where(a => a.UserId == request.UserId.Value);

            if (request.From.HasValue)
                query = query.Where(a => a.Timestamp >= request.From.Value);

            if (request.To.HasValue)
                query = query.Where(a => a.Timestamp <= request.To.Value);

            return await query
                .OrderByDescending(a => a.Timestamp)
                .Take(request.Limit)
                .Select(a => new AuditLogDto(
                    a.Id, a.EntityType, a.EntityId, a.Action,
                    a.OldValues, a.NewValues, a.UserId, a.Timestamp))
                .ToListAsync(cancellationToken);
        }
    }
}
