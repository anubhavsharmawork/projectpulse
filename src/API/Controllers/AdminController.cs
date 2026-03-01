using Application.Admin.Queries;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    /// <summary>
    /// Admin endpoints for system configuration (roles, permissions).
    /// Access restricted to users with Admin.ManageRoles permission.
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/admin")]
    [Authorize(Policy = "AdminManageRolesPolicy")]
    public class AdminController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AdminController(IMediator mediator) => _mediator = mediator;

        /// <summary>
        /// Get all system roles with their permissions grouped by category.
        /// Reads from database (originally seeded from RolePermissions.json).
        /// </summary>
        [HttpGet("roles")]
        public async Task<IActionResult> GetRoles()
        {
            var result = await _mediator.Send(new GetRolesQuery());
            return Ok(result);
        }
    }
}
