using Application.WorkItems.Commands;
using Application.Tasks.Commands;
using Domain.Entities;
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
    [Route("api/v{version:apiVersion}/projects/{projectId:guid}/work-items")]
    [Authorize(Policy = "MemberPolicy")]
    public class WorkItemsController : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetAll(Guid projectId, [FromServices] AppDbContext db)
            => Ok(await db.WorkItems
                .AsNoTracking()
                .Where(w => w.ProjectId == projectId)
                .Select(w => new
                {
                    w.Id,
                    w.ProjectId,
                    w.ParentId,
                    w.Title,
                    w.Description,
                    w.AttachmentUrl,
                    w.IsCompleted,
                    w.AssigneeId,
                    w.CreatedAt,
                    w.CompletedAt,
                    w.Type
                })
                .ToListAsync());

        [HttpGet("epics")]
        public async Task<IActionResult> GetEpics(Guid projectId, [FromServices] AppDbContext db)
            => Ok(await db.WorkItems
                .OfType<EpicWorkItem>()
                .AsNoTracking()
                .Where(w => w.ProjectId == projectId)
                .Select(w => new
                {
                    w.Id,
                    w.ProjectId,
                    w.Title,
                    w.Description,
                    w.AttachmentUrl,
                    w.IsCompleted,
                    w.AssigneeId,
                    w.CreatedAt,
                    w.CompletedAt
                })
                .ToListAsync());

        [HttpGet("user-stories")]
        public async Task<IActionResult> GetUserStories(Guid projectId, [FromServices] AppDbContext db)
            => Ok(await db.WorkItems
                .OfType<UserStoryWorkItem>()
                .AsNoTracking()
                .Where(w => w.ProjectId == projectId)
                .Select(w => new
                {
                    w.Id,
                    w.ProjectId,
                    w.ParentId,
                    w.Title,
                    w.Description,
                    w.AttachmentUrl,
                    w.IsCompleted,
                    w.AssigneeId,
                    w.CreatedAt,
                    w.CompletedAt
                })
                .ToListAsync());

        [HttpGet("user-stories/{userStoryId:guid}/tasks")]
        public async Task<IActionResult> GetTasksForUserStory(Guid projectId, Guid userStoryId, [FromServices] AppDbContext db)
            => Ok(await db.WorkItems
                .OfType<TaskWorkItem>()
                .AsNoTracking()
                .Where(t => t.ProjectId == projectId && t.ParentId == userStoryId)
                .Select(t => new
                {
                    t.Id,
                    t.ProjectId,
                    t.ParentId,
                    t.Title,
                    t.Description,
                    t.AttachmentUrl,
                    t.IsCompleted,
                    t.AssigneeId,
                    t.CreatedAt,
                    t.CompletedAt,
                    t.Type
                })
                .ToListAsync());

        [HttpPost("epics")]
        public async Task<IActionResult> CreateEpic(Guid projectId, CreateEpicCommand cmd, [FromServices] IMediator mediator)
        {
            cmd = cmd with { ProjectId = projectId };
            return Ok(await mediator.Send(cmd));
        }

        [HttpPost("user-stories")]
        public async Task<IActionResult> CreateUserStory(Guid projectId, CreateUserStoryCommand cmd, [FromServices] IMediator mediator)
        {
            cmd = cmd with { ProjectId = projectId };
            return Ok(await mediator.Send(cmd));
        }

        [HttpPost("user-stories/{userStoryId:guid}/tasks")]
        public async Task<IActionResult> CreateTaskForUserStory(Guid projectId, Guid userStoryId, CreateTaskCommand cmd, [FromServices] IMediator mediator)
        {
            cmd = cmd with { ProjectId = projectId, ParentId = userStoryId };
            return Ok(await mediator.Send(cmd));
        }

        [HttpGet("{workItemId:guid}")]
        public async Task<IActionResult> GetById(Guid projectId, Guid workItemId, [FromServices] AppDbContext db)
        {
            var item = await db.WorkItems
                .AsNoTracking()
                .Where(w => w.Id == workItemId && w.ProjectId == projectId)
                .Select(w => new
                {
                    w.Id,
                    w.ProjectId,
                    w.ParentId,
                    w.Title,
                    w.Description,
                    w.AttachmentUrl,
                    w.IsCompleted,
                    w.AssigneeId,
                    w.CreatedAt,
                    w.CompletedAt,
                    w.Type
                })
                .FirstOrDefaultAsync();
            return item == null ? NotFound() : Ok(item);
        }

        [HttpGet("{workItemId:guid}/children")]
        public async Task<IActionResult> GetChildren(Guid projectId, Guid workItemId, [FromServices] AppDbContext db)
            => Ok(await db.WorkItems
                .AsNoTracking()
                .Where(w => w.ParentId == workItemId && w.ProjectId == projectId)
                .Select(w => new
                {
                    w.Id,
                    w.ProjectId,
                    w.ParentId,
                    w.Title,
                    w.Description,
                    w.IsCompleted,
                    w.Type
                })
                .ToListAsync());

        [HttpDelete("{workItemId:guid}")]
        public async Task<IActionResult> Delete(Guid projectId, Guid workItemId, [FromServices] AppDbContext db)
        {
            var e = await db.WorkItems.FirstOrDefaultAsync(w => w.Id == workItemId && w.ProjectId == projectId);
            if (e == null) return NotFound();
            db.Remove(e);
            await db.SaveChangesAsync();
            return NoContent();
        }
    }
}
