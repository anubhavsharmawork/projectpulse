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
        /// Get users for mention autocomplete. Optionally filter by search term.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetUsers([FromQuery] string? search)
        {
            var result = await _mediator.Send(new GetUsersQuery(search));
            return Ok(result);
        }
    }
}
