using Application.Common.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Application.Notifications.Commands
{
    public record MarkNotificationReadCommand(Guid NotificationId) : IRequest<Unit>;

    public class MarkNotificationReadHandler : IRequestHandler<MarkNotificationReadCommand, Unit>
    {
        private readonly IAppDbContext _db;
        private readonly IHttpContextAccessor _http;

        public MarkNotificationReadHandler(IAppDbContext db, IHttpContextAccessor http)
        {
            _db = db;
            _http = http;
        }

        public async Task<Unit> Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
        {
            var userIdClaim = _http.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userId = Guid.TryParse(userIdClaim, out var id) ? id : Guid.Empty;

            var notification = await _db.Notifications
                .FirstOrDefaultAsync(n => n.Id == request.NotificationId && n.UserId == userId, cancellationToken);

            if (notification is null)
                throw new InvalidOperationException("Notification not found");

            notification.IsRead = true;
            await _db.SaveChangesAsync(cancellationToken);
            return Unit.Value;
        }
    }

    public record MarkAllNotificationsReadCommand() : IRequest<int>;

    public class MarkAllNotificationsReadHandler : IRequestHandler<MarkAllNotificationsReadCommand, int>
    {
        private readonly IAppDbContext _db;
        private readonly IHttpContextAccessor _http;

        public MarkAllNotificationsReadHandler(IAppDbContext db, IHttpContextAccessor http)
        {
            _db = db;
            _http = http;
        }

        public async Task<int> Handle(MarkAllNotificationsReadCommand request, CancellationToken cancellationToken)
        {
            var userIdClaim = _http.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userId = Guid.TryParse(userIdClaim, out var id) ? id : Guid.Empty;

            var unread = await _db.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync(cancellationToken);

            foreach (var n in unread)
                n.IsRead = true;

            await _db.SaveChangesAsync(cancellationToken);
            return unread.Count;
        }
    }
}
