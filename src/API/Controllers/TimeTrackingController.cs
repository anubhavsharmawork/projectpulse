using Application.TimeTracking.Commands;
using Application.TimeTracking.Queries;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/time-entries")]
    [Authorize(Policy = "MemberPolicy")]
    public class TimeTrackingController : ControllerBase
    {
        private readonly IMediator _mediator;

        public TimeTrackingController(IMediator mediator) => _mediator = mediator;

        [HttpPost]
        public async Task<IActionResult> LogTime([FromBody] LogTimeEntryCommand cmd)
        {
            try
            {
                return Ok(new { timeEntryId = await _mediator.Send(cmd) });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { title = "Forbidden", detail = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { title = "Bad Request", detail = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetTimeEntries(
            [FromQuery] Guid? workItemId,
            [FromQuery] Guid? userId,
            [FromQuery] Guid? projectId,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
            => Ok(await _mediator.Send(new GetTimeEntriesQuery(workItemId, userId, projectId, from, to)));
    }
}
