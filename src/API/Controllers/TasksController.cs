using Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using API.Hubs;

namespace API.Controllers
{
    [ApiController]
    [Route("api/projects/{projectId:guid}/[controller]")]
    [Authorize(Policy = "MemberPolicy")]
    public class TasksController : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetAll(Guid projectId, [FromServices] AppDbContext db)
            => Ok(await db.Tasks
                .AsNoTracking()
                .Where(t => t.ProjectId == projectId)
                .Select(t => new {
                    t.Id,
                    t.ProjectId,
                    t.Title,
                    t.Description,
                    t.AttachmentUrl,
                    t.IsCompleted,
                    t.AssigneeId,
                    t.CreatedAt,
                    t.CompletedAt
                })
                .ToListAsync());

        [HttpPost]
        public async Task<IActionResult> Create(Guid projectId, Application.Tasks.Commands.CreateTaskCommand cmd, [FromServices] IMediator mediator)
        {
            cmd = cmd with { ProjectId = projectId };
            return Ok(await mediator.Send(cmd));
        }

        [HttpPost("{taskId:guid}/complete")]
        public async Task<IActionResult> Complete(Guid projectId, Guid taskId, [FromServices] AppDbContext db, [FromServices] IHubContext<ProjectHub> hub)
        {
            var e = await db.Tasks.FirstOrDefaultAsync(t => t.Id == taskId && t.ProjectId == projectId);
            if (e == null) return NotFound();
            if (!e.IsCompleted)
            {
                e.IsCompleted = true;
                e.CompletedAt = DateTime.UtcNow;
                await db.SaveChangesAsync();
                // Broadcast update so clients can refresh summaries
                await hub.Clients.All.SendAsync("TaskUpdated", new { ProjectId = projectId, TaskId = taskId, IsCompleted = true, CompletedAt = e.CompletedAt });
            }
            return NoContent();
        }

        [HttpDelete("{taskId:guid}")]
        public async Task<IActionResult> Delete(Guid projectId, Guid taskId, [FromServices] AppDbContext db)
        {
            var e = await db.Tasks.FirstOrDefaultAsync(t => t.Id == taskId && t.ProjectId == projectId);
            if (e == null) return NotFound();
            db.Remove(e);
            await db.SaveChangesAsync();
            return NoContent();
        }
    }
}
