using Application.Common.Interfaces;
using Domain.Entities;

namespace Application.Notifications
{
    public static class NotificationHelpers
    {
        public static void AddNotification(
            this IAppDbContext db,
            Guid userId,
            NotificationType type,
            string message,
            Guid? relatedEntityId = null)
        {
            db.Notifications.Add(new Notification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Type = type,
                Message = message,
                IsRead = false,
                RelatedEntityId = relatedEntityId
            });
        }
    }
}
