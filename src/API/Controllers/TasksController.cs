using Domain.Entities;
using Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using API.Hubs;
using Asp.Versioning;

namespace API.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/projects/{projectId:guid}/[controller]")]
    [Authorize(Policy = "MemberPolicy")]
    public class TasksController : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetAll(Guid projectId, [FromQuery] bool orphansOnly, [FromServices] AppDbContext db)
        {
            var query = db.WorkItems
                .OfType<TaskWorkItem>()
                .AsNoTracking()
                .Where(t => t.ProjectId == projectId);

            if (orphansOnly)
            {
                query = query.Where(t => t.ParentId == null);
            }

            return Ok(await query
                .Select(t => new {
                    t.Id,
                    t.ProjectId,
                    t.Title,
                    t.Description,
                    t.AttachmentUrl,
                    t.IsCompleted,
                    t.AssigneeId,
                    t.CreatedAt,
                    t.CompletedAt,
                    t.ParentId
                })
                .ToListAsync());
        }

        [HttpPost]
        public async Task<IActionResult> Create(Guid projectId, Application.Tasks.Commands.CreateTaskCommand cmd, [FromServices] IMediator mediator)
        {
            cmd = cmd with { ProjectId = projectId };
            return Ok(await mediator.Send(cmd));
        }

        [HttpPost("{taskId:guid}/complete")]
        public async Task<IActionResult> Complete(Guid projectId, Guid taskId, [FromServices] AppDbContext db, [FromServices] IHubContext<ProjectHub> hub)
        {
            var e = await db.WorkItems.OfType<TaskWorkItem>().FirstOrDefaultAsync(t => t.Id == taskId && t.ProjectId == projectId);
            if (e == null) return NotFound();
            if (!e.IsCompleted)
            {
                e.IsCompleted = true;
                e.CompletedAt = DateTime.UtcNow;
                await db.SaveChangesAsync();
                await hub.Clients.All.SendAsync("TaskUpdated", new { ProjectId = projectId, TaskId = taskId, IsCompleted = true, CompletedAt = e.CompletedAt });
            }
            return NoContent();
        }

        [HttpDelete("{taskId:guid}")]
        public async Task<IActionResult> Delete(Guid projectId, Guid taskId, [FromServices] AppDbContext db)
        {
            var e = await db.WorkItems.OfType<TaskWorkItem>().FirstOrDefaultAsync(t => t.Id == taskId && t.ProjectId == projectId);
            if (e == null) return NotFound();
            db.Remove(e);
            await db.SaveChangesAsync();
            return NoContent();
        }
    }
}
