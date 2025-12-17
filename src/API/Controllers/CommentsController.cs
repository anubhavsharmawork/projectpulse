using Application.Comments.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Persistence;
using Asp.Versioning;

namespace API.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/work-items/{workItemId:guid}/[controller]")]
    [Authorize(Policy = "MemberPolicy")]
    public class CommentsController : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetAll(Guid workItemId, [FromServices] AppDbContext db)
            => Ok(await db.Comments.AsNoTracking().Where(c => c.WorkItemId == workItemId).ToListAsync());

        [HttpPost]
        public async Task<IActionResult> Create(Guid workItemId, CreateCommentCommand cmd, [FromServices] IMediator mediator)
        {
            cmd = cmd with { WorkItemId = workItemId };
            return Ok(await mediator.Send(cmd));
        }

        [HttpDelete("{commentId:guid}")]
        public async Task<IActionResult> Delete(Guid workItemId, Guid commentId, [FromServices] AppDbContext db)
        {
            var e = await db.Comments.FirstOrDefaultAsync(c => c.Id == commentId && c.WorkItemId == workItemId);
            if (e == null) return NotFound();
            db.Remove(e);
            await db.SaveChangesAsync();
            return NoContent();
        }
    }
}
