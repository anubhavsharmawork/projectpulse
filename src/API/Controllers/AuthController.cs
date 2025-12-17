using Application.Auth.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;

namespace API.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class AuthController : ControllerBase
    {
        // JSON register
        [AllowAnonymous]
        [HttpPost("register")]
        [Consumes("application/json")]
        public async Task<IActionResult> RegisterJson([FromBody] RegisterUserCommand cmd, [FromServices] IMediator mediator)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            try { return Ok(await mediator.Send(cmd)); }
            catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        }

        // x-www-form-urlencoded register (optional)
        [AllowAnonymous]
        [HttpPost("register")]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<IActionResult> RegisterForm([FromForm] RegisterUserCommand cmd, [FromServices] IMediator mediator)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            try { return Ok(await mediator.Send(cmd)); }
            catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        }

        // JSON login
        [AllowAnonymous]
        [HttpPost("login")]
        [Consumes("application/json")]
        public async Task<IActionResult> LoginJson([FromBody] LoginUserCommand cmd, [FromServices] IMediator mediator)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            try { return Ok(await mediator.Send(cmd)); }
            catch (UnauthorizedAccessException) { return Unauthorized(); }
        }

        // x-www-form-urlencoded login (optional)
        [AllowAnonymous]
        [HttpPost("login")]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<IActionResult> LoginForm([FromForm] LoginUserCommand cmd, [FromServices] IMediator mediator)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            try { return Ok(await mediator.Send(cmd)); }
            catch (UnauthorizedAccessException) { return Unauthorized(); }
        }
    }
}
