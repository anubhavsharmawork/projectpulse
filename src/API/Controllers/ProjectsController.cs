using Application.Common.Interfaces;
using Application.Projects.Queries;
using Application.Workflows.Commands;
using Application.Workflows.Queries;
using Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Asp.Versioning;
using System.Security.Claims;

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
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userId = Guid.TryParse(userIdClaim, out var id) ? id : Guid.Empty;

            var projects = await db.Projects
                .AsNoTracking()
                .Where(p => p.IsPublic || p.OwnerId == userId)
                .ToListAsync();

            return Ok(projects);
        }

        [HttpGet("public")]
        public async Task<IActionResult> GetPublic([FromServices] AppDbContext db)
            => Ok(await db.Projects.AsNoTracking().Where(p => p.IsPublic).ToListAsync());

        [HttpGet("mine")]
        public async Task<IActionResult> GetMine([FromServices] AppDbContext db)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userId = Guid.TryParse(userIdClaim, out var id) ? id : Guid.Empty;

            var projects = await db.Projects
                .AsNoTracking()
                .Where(p => p.OwnerId == userId)
                .ToListAsync();

            return Ok(projects);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Application.Projects.Commands.CreateProjectCommand cmd, [FromServices] IMediator mediator)
            => Ok(await mediator.Send(cmd));

        [HttpGet("{id:guid}/config")]
        public async Task<IActionResult> GetConfig(Guid id, [FromServices] IMediator mediator)
            => Ok(await mediator.Send(new GetProjectConfigQuery(id)));

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, [FromServices] AppDbContext db)
        {
            var e = await db.Projects.FindAsync(id);
            if (e == null) return NotFound();
            db.Remove(e);
            await db.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("{projectId:guid}/workflow")]
        public async Task<IActionResult> GetWorkflow(Guid projectId, [FromServices] IMediator mediator)
        {
            var result = await mediator.Send(new GetProjectWorkflowQuery(projectId));
            if (result is null) return NotFound();
            return Ok(result);
        }

        [HttpPut("{projectId:guid}/workflow")]
        public async Task<IActionResult> UpdateWorkflow(Guid projectId, [FromBody] UpdateProjectWorkflowCommand cmd, [FromServices] IMediator mediator, [FromServices] ILogger<ProjectsController> logger)
        {
            if (cmd.ProjectId != projectId)
                return BadRequest(new { detail = "Project ID in URL does not match body." });

            try
            {
                var workflowId = await mediator.Send(cmd);
                return Ok(new { workflowId });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { title = "Forbidden", detail = ex.Message });
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency conflict saving workflow for project {ProjectId}", projectId);
                return Conflict(new { title = "Conflict", detail = "The workflow was modified by another user. Please refresh the page and try again." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { title = "Bad Request", detail = ex.Message });
            }
        }

        [HttpPost("{projectId:guid}/work-items/{workItemId:guid}/state")]
        public async Task<IActionResult> ChangeWorkItemState(
            Guid projectId, Guid workItemId,
            [FromBody] ChangeStateRequest request,
            [FromServices] IMediator mediator,
            [FromServices] IWorkflowEngine workflowEngine,
            [FromServices] AppDbContext db)
        {
            try
            {
                // Check if the work item currently has a state assigned
                var workItem = await db.WorkItems
                    .AsNoTracking()
                    .FirstOrDefaultAsync(w => w.Id == workItemId && w.ProjectId == projectId);

                if (workItem is null)
                    return NotFound(new { title = "Not Found", detail = "Work item not found" });

                if (workItem.CurrentStateId is null)
                {
                    // No state yet — assign the initial state directly
                    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    var userId = Guid.TryParse(userIdClaim, out var id) ? id : Guid.Empty;

                    await workflowEngine.AssignInitialStateAsync(
                        workItemId, request.TargetStateId, userId);

                    return Ok(new { transitionId = Guid.Empty });
                }

                // Already has a state — use the regular transition engine
                var result = await mediator.Send(
                    new TransitionWorkItemStateCommand(workItemId, request.TargetStateId, request.Comment));
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { title = "Forbidden", detail = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { title = "Bad Request", detail = ex.Message });
            }
        }
    }

    public record ChangeStateRequest(Guid TargetStateId, string? Comment = null);
}
