using Application.Teams.Commands;
using Application.Teams.Queries;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize(Policy = "MemberPolicy")]
    public class TeamsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public TeamsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Get all members of a team.
        /// </summary>
        [HttpGet("{teamId:guid}/members")]
        [Authorize(Policy = "TeamViewCapacityPolicy")]
        public async Task<IActionResult> GetMembers(Guid teamId)
        {
            var result = await _mediator.Send(new GetTeamMembersQuery(teamId));
            return Ok(result);
        }

        /// <summary>
        /// Get team capacity overview (workload, utilization, task counts).
        /// </summary>
        [HttpGet("{teamId:guid}/capacity")]
        [Authorize(Policy = "TeamViewCapacityPolicy")]
        public async Task<IActionResult> GetCapacity(Guid teamId)
        {
            var result = await _mediator.Send(new GetTeamCapacityQuery(teamId));
            return Ok(result);
        }

        /// <summary>
        /// Add a member to a team.
        /// </summary>
        [HttpPost("{teamId:guid}/members")]
        [Authorize(Policy = "TeamAddMembersPolicy")]
        public async Task<IActionResult> AddMember(Guid teamId, [FromBody] AddTeamMemberRequest request)
        {
            var cmd = new CreateTeamMemberCommand(
                teamId,
                request.UserId,
                request.Role,
                request.DomainExpertise,
                request.Skills,
                request.AvailabilityHoursPerWeek,
                request.CostRate);

            var result = await _mediator.Send(cmd);
            return CreatedAtAction(nameof(GetMembers), new { teamId }, result);
        }

        /// <summary>
        /// Update an existing team member.
        /// </summary>
        [HttpPut("members/{teamMemberId:guid}")]
        [Authorize(Policy = "TeamChangeRolesPolicy")]
        public async Task<IActionResult> UpdateMember(Guid teamMemberId, [FromBody] UpdateTeamMemberRequest request)
        {
            var cmd = new UpdateTeamMemberCommand(
                teamMemberId,
                request.Role,
                request.DomainExpertise,
                request.Skills,
                request.AvailabilityHoursPerWeek,
                request.CostRate);

            await _mediator.Send(cmd);
            return NoContent();
        }

        /// <summary>
        /// Remove a member from a team.
        /// </summary>
        [HttpDelete("members/{teamMemberId:guid}")]
        [Authorize(Policy = "TeamRemoveMembersPolicy")]
        public async Task<IActionResult> RemoveMember(Guid teamMemberId)
        {
            await _mediator.Send(new RemoveTeamMemberCommand(teamMemberId));
            return NoContent();
        }

        /// <summary>
        /// Assign a user to a project by username (auto-creates team if needed).
        /// </summary>
        [HttpPost("projects/{projectId:guid}/assign")]
        [Authorize(Policy = "TeamAddMembersPolicy")]
        public async Task<IActionResult> AssignToProject(Guid projectId, [FromBody] AssignToProjectRequest request)
        {
            var cmd = new AssignToProjectCommand(
                projectId,
                request.Username,
                request.Role,
                request.DomainExpertise,
                request.Skills,
                request.AvailabilityHoursPerWeek,
                request.CostRate);

            try
            {
                var result = await _mediator.Send(cmd);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { title = "Assignment failed", detail = ex.Message });
            }
        }

        /// <summary>
        /// Get team members for a project (looks up team by project).
        /// </summary>
        [HttpGet("projects/{projectId:guid}/members")]
        [Authorize(Policy = "MemberPolicy")]
        public async Task<IActionResult> GetMembersByProject(Guid projectId)
        {
            var result = await _mediator.Send(new GetTeamMembersByProjectQuery(projectId));
            return Ok(result);
        }

        /// <summary>
        /// Get available roles for a project (domain-scoped).
        /// </summary>
        [HttpGet("projects/{projectId:guid}/roles")]
        [Authorize(Policy = "MemberPolicy")]
        public async Task<IActionResult> GetProjectRoles(Guid projectId)
        {
            var result = await _mediator.Send(new GetProjectRolesQuery(projectId));
            return Ok(result);
        }

        /// <summary>
        /// Unassign a user from a project.
        /// </summary>
        [HttpPost("projects/{projectId:guid}/unassign")]
        [Authorize(Policy = "TeamRemoveMembersPolicy")]
        public async Task<IActionResult> UnassignFromProject(Guid projectId, [FromBody] UnassignFromProjectRequest request)
        {
            await _mediator.Send(new UnassignFromProjectCommand(projectId, request.UserId));
            return NoContent();
        }
    }

    // ── Request DTOs ──

    public record AddTeamMemberRequest(
        Guid UserId,
        string Role,
        string? DomainExpertise,
        string? Skills,
        decimal AvailabilityHoursPerWeek = 40,
        decimal CostRate = 0);

    public record UpdateTeamMemberRequest(
        string Role,
        string? DomainExpertise,
        string? Skills,
        decimal AvailabilityHoursPerWeek = 40,
        decimal CostRate = 0);

    public record AssignToProjectRequest(
        string Username,
        string Role,
        string? DomainExpertise,
        string? Skills,
        decimal AvailabilityHoursPerWeek = 40,
        decimal CostRate = 0);

    public record UnassignFromProjectRequest(Guid UserId);
}
