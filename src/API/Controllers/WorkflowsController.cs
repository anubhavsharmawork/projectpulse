using Application.Workflows.Commands;
using Application.Workflows.Queries;
using Asp.Versioning;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize(Policy = "MemberPolicy")]
    public class WorkflowsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<WorkflowsController> _logger;

        public WorkflowsController(IMediator mediator, ILogger<WorkflowsController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        /// <summary>
        /// Get all domains with their default workflow availability.
        /// </summary>
        [HttpGet("domains")]
        public async Task<IActionResult> GetDomains()
        {
            try
            {
                _logger.LogInformation("Fetching workflow domains.");
                var result = await _mediator.Send(new GetWorkflowDomainsQuery());
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch workflow domains.");
                return StatusCode(500, new { error = "Failed to load workflow domains.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Get the workflow definition for a domain type.
        /// </summary>
        [HttpGet("domain/{domainType}")]
        public async Task<IActionResult> GetByDomain(string domainType)
        {
            if (!Enum.TryParse<DomainType>(domainType, ignoreCase: true, out var parsed))
                return BadRequest($"Unknown domain type: {domainType}");

            try
            {
                _logger.LogInformation("Fetching workflow for domain: {DomainType}", domainType);
                var result = await _mediator.Send(new GetWorkflowByDomainQuery(parsed));
                if (result is null)
                {
                    _logger.LogInformation("No workflow found for domain: {DomainType}", domainType);
                    return NotFound();
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch workflow for domain: {DomainType}", domainType);
                return StatusCode(500, new { error = "Failed to load workflow.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Get available transitions for a work item from its current state.
        /// </summary>
        [HttpGet("work-items/{workItemId:guid}/transitions")]
        public async Task<IActionResult> GetAvailableTransitions(Guid workItemId)
        {
            try
            {
                var result = await _mediator.Send(new GetAvailableTransitionsQuery(workItemId));
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch transitions for work item: {WorkItemId}", workItemId);
                return StatusCode(500, new { error = "Failed to load transitions.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Transition a work item to a new workflow state.
        /// </summary>
        [HttpPost("work-items/{workItemId:guid}/transition")]
        public async Task<IActionResult> TransitionState(Guid workItemId, [FromBody] TransitionRequest request)
        {
            try
            {
                var cmd = new TransitionWorkItemStateCommand(
                    workItemId,
                    request.TargetStateId,
                    request.Comment);

                var result = await _mediator.Send(cmd);
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to transition work item: {WorkItemId}", workItemId);
                return StatusCode(500, new { error = "Failed to transition work item.", detail = ex.Message });
            }
        }
    }

    public record TransitionRequest(Guid TargetStateId, string? Comment = null);
}
