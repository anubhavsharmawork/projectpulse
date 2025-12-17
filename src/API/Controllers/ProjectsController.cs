using Application.Common.Interfaces;
using Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Asp.Versioning;

namespace API.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize(Policy = "MemberPolicy")]
    public class ProjectsController : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetAll([FromServices] AppDbContext db)
            => Ok(await db.Projects.AsNoTracking().ToListAsync());

        [HttpPost]
        public async Task<IActionResult> Create(Application.Projects.Commands.CreateProjectCommand cmd, [FromServices] IMediator mediator)
            => Ok(await mediator.Send(cmd));

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, [FromServices] AppDbContext db)
        {
            var e = await db.Projects.FindAsync(id);
            if (e == null) return NotFound();
            db.Remove(e);
            await db.SaveChangesAsync();
            return NoContent();
        }
    }
}
