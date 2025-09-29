using Application.Comments.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Persistence;

namespace API.Controllers
{
    [ApiController]
    [Route("api/tasks/{taskId:guid}/[controller]")]
    [Authorize(Policy = "MemberPolicy")]
    public class CommentsController : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetAll(Guid taskId, [FromServices] AppDbContext db)
            => Ok(await db.Comments.AsNoTracking().Where(c => c.TaskItemId == taskId).ToListAsync());

        [HttpPost]
        public async Task<IActionResult> Create(Guid taskId, CreateCommentCommand cmd, [FromServices] IMediator mediator)
        {
            cmd = cmd with { TaskId = taskId };
            return Ok(await mediator.Send(cmd));
        }

        [HttpDelete("{commentId:guid}")]
        public async Task<IActionResult> Delete(Guid taskId, Guid commentId, [FromServices] AppDbContext db)
        {
            var e = await db.Comments.FirstOrDefaultAsync(c => c.Id == commentId && c.TaskItemId == taskId);
            if (e == null) return NotFound();
            db.Remove(e);
            await db.SaveChangesAsync();
            return NoContent();
        }
    }
}
