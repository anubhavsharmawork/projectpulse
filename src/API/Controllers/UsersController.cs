using Application.Users.Commands;
using Application.Users.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;

namespace API.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize(Policy = "MemberPolicy")]
    public class UsersController : ControllerBase
    {
        private readonly IMediator _mediator;

        public UsersController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Get users for admin purposes. Restricted to admin users only.
        /// </summary>
        [HttpGet]
        [Authorize(Policy = "AdminPolicy")]
        public async Task<IActionResult> GetUsers([FromQuery] string? search)
        {
            var result = await _mediator.Send(new GetUsersQuery(search));
            return Ok(result);
        }

        /// <summary>
        /// Resolve a username to verify it exists. Returns minimal info (display name only, no email).
        /// Used by Team Management to validate a username before assignment.
        /// </summary>
        [HttpPost("resolve")]
        public async Task<IActionResult> ResolveUsername([FromBody] ResolveUsernameRequest request)
        {
            var result = await _mediator.Send(new ResolveUsernameQuery(request.Username));
            if (result is null)
                return NotFound(new { title = "User not found", detail = $"No user found with username '{request.Username}'" });
            return Ok(result);
        }

        /// <summary>
        /// Update the current user's timezone preference.
        /// </summary>
        [HttpPut("timezone")]
        public async Task<IActionResult> UpdateTimezone([FromBody] UpdateTimezoneRequest request)
        {
            await _mediator.Send(new UpdateTimezoneCommand(request.TimeZoneId, request.TimeZoneOffset));
            return Ok(new { updated = true });
        }
    }

    public record ResolveUsernameRequest(string Username);
    public record UpdateTimezoneRequest(string TimeZoneId, int TimeZoneOffset);
}
