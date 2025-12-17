using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Asp.Versioning;

namespace API.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize(Policy = "MemberPolicy")]
    public class MentionNotificationsController : ControllerBase
    {
        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdClaim, out var id) ? id : Guid.Empty;
        }

        /// <summary>
        /// Get all mention notifications for the current user
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetMyNotifications([FromServices] AppDbContext db)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var notifications = await db.MentionNotifications
                .AsNoTracking()
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(50)
                .Select(n => new
                {
                    n.Id,
                    n.CommentId,
                    n.WorkItemId,
                    n.CommentBody,
                    n.WorkItemTitle,
                    n.MentionedByName,
                    n.IsRead,
                    n.CreatedAt
                })
                .ToListAsync();

            return Ok(notifications);
        }

        /// <summary>
        /// Get unread notification count for the current user
        /// </summary>
        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount([FromServices] AppDbContext db)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var count = await db.MentionNotifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);

            return Ok(new { count });
        }

        /// <summary>
        /// Mark a notification as read
        /// </summary>
        [HttpPost("{id:guid}/read")]
        public async Task<IActionResult> MarkAsRead(Guid id, [FromServices] AppDbContext db)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var notification = await db.MentionNotifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

            if (notification == null) return NotFound();

            notification.IsRead = true;
            await db.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Mark all notifications as read for the current user
        /// </summary>
        [HttpPost("read-all")]
        public async Task<IActionResult> MarkAllAsRead([FromServices] AppDbContext db)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var unreadNotifications = await db.MentionNotifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
            }

            await db.SaveChangesAsync();
            return NoContent();
        }
    }
}
