using Application.Common.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Application.Notifications.Queries
{
    public record GetUnreadNotificationsQuery() : IRequest<List<NotificationDto>>;

    public record NotificationDto(
        Guid Id,
        string Type,
        string Message,
        bool IsRead,
        DateTime CreatedAt,
        Guid? RelatedEntityId);

    public class GetUnreadNotificationsHandler : IRequestHandler<GetUnreadNotificationsQuery, List<NotificationDto>>
    {
        private readonly IAppDbContext _db;
        private readonly IHttpContextAccessor _http;

        public GetUnreadNotificationsHandler(IAppDbContext db, IHttpContextAccessor http)
        {
            _db = db;
            _http = http;
        }

        public async Task<List<NotificationDto>> Handle(GetUnreadNotificationsQuery request, CancellationToken cancellationToken)
        {
            var userIdClaim = _http.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userId = Guid.TryParse(userIdClaim, out var id) ? id : Guid.Empty;

            return await _db.Notifications
                .AsNoTracking()
                .Where(n => n.UserId == userId && !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .Take(100)
                .Select(n => new NotificationDto(
                    n.Id, n.Type.ToString(), n.Message,
                    n.IsRead, n.CreatedAt, n.RelatedEntityId))
                .ToListAsync(cancellationToken);
        }
    }
}
