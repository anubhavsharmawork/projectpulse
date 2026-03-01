using Application.Notifications.Commands;
using Application.Notifications.Queries;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize(Policy = "MemberPolicy")]
    public class NotificationsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public NotificationsController(IMediator mediator) => _mediator = mediator;

        [HttpGet("unread")]
        public async Task<IActionResult> GetUnread()
            => Ok(await _mediator.Send(new GetUnreadNotificationsQuery()));

        [HttpPost("{id:guid}/read")]
        public async Task<IActionResult> MarkRead(Guid id)
        {
            await _mediator.Send(new MarkNotificationReadCommand(id));
            return NoContent();
        }

        [HttpPost("read-all")]
        public async Task<IActionResult> MarkAllRead()
        {
            var count = await _mediator.Send(new MarkAllNotificationsReadCommand());
            return Ok(new { markedRead = count });
        }
    }
}
