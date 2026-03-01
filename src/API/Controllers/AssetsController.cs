using Application.Assets.Commands;
using Application.Assets.Queries;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;

namespace API.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}")]
    [Authorize(Policy = "MemberPolicy")]
    public class AssetsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AssetsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Lists assets for a project with optional filtering and pagination.
        /// </summary>
        [HttpGet("projects/{projectId:guid}/assets")]
        public async Task<IActionResult> GetByProject(
            Guid projectId,
            [FromQuery] AssetStatus? status,
            [FromQuery] AssetType? type,
            [FromQuery] string? search,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var result = await _mediator.Send(new GetAssetsByProjectQuery(
                projectId, status, type, search, page, pageSize));
            return Ok(result);
        }

        /// <summary>
        /// Gets a single asset by ID with full details.
        /// </summary>
        [HttpGet("assets/{assetId:guid}")]
        public async Task<IActionResult> GetById(Guid assetId)
        {
            var result = await _mediator.Send(new GetAssetByIdQuery(assetId));
            return result is null ? NotFound() : Ok(result);
        }

        /// <summary>
        /// Creates a new physical asset within a project.
        /// </summary>
        [HttpPost("projects/{projectId:guid}/assets")]
        public async Task<IActionResult> Create(Guid projectId, CreateAssetCommand cmd)
        {
            cmd = cmd with { ProjectId = projectId };
            var result = await _mediator.Send(cmd);
            return CreatedAtAction(nameof(GetById), new { assetId = result.AssetId }, result);
        }

        /// <summary>
        /// Updates an existing asset.
        /// </summary>
        [HttpPut("assets/{assetId:guid}")]
        public async Task<IActionResult> Update(Guid assetId, UpdateAssetCommand cmd)
        {
            cmd = cmd with { AssetId = assetId };
            await _mediator.Send(cmd);
            return NoContent();
        }

        /// <summary>
        /// Soft-deletes an asset.
        /// </summary>
        [HttpDelete("assets/{assetId:guid}")]
        [Authorize(Policy = "AdminPolicy")]
        public async Task<IActionResult> Delete(Guid assetId)
        {
            await _mediator.Send(new DeleteAssetCommand(assetId));
            return NoContent();
        }

        /// <summary>
        /// Assigns an asset to a user.
        /// </summary>
        [HttpPost("assets/{assetId:guid}/assign")]
        public async Task<IActionResult> Assign(Guid assetId, AssignAssetCommand cmd)
        {
            cmd = cmd with { AssetId = assetId };
            var result = await _mediator.Send(cmd);
            return Ok(result);
        }

        /// <summary>
        /// Returns an assigned asset.
        /// </summary>
        [HttpPost("assets/{assetId:guid}/return")]
        public async Task<IActionResult> Return(Guid assetId, ReturnAssetCommand cmd)
        {
            cmd = cmd with { AssetId = assetId };
            await _mediator.Send(cmd);
            return Ok();
        }

        /// <summary>
        /// Gets maintenance history for an asset.
        /// </summary>
        [HttpGet("assets/{assetId:guid}/maintenance")]
        public async Task<IActionResult> GetMaintenanceHistory(Guid assetId)
        {
            var result = await _mediator.Send(new GetAssetMaintenanceHistoryQuery(assetId));
            return Ok(result);
        }

        /// <summary>
        /// Schedules a new maintenance record for an asset.
        /// </summary>
        [HttpPost("assets/{assetId:guid}/maintenance")]
        public async Task<IActionResult> ScheduleMaintenance(Guid assetId, ScheduleMaintenanceCommand cmd)
        {
            cmd = cmd with { AssetId = assetId };
            var result = await _mediator.Send(cmd);
            return CreatedAtAction(nameof(GetMaintenanceHistory), new { assetId }, result);
        }

        /// <summary>
        /// Records a completed maintenance event.
        /// </summary>
        [HttpPut("assets/maintenance/{maintenanceRecordId:guid}")]
        public async Task<IActionResult> RecordMaintenance(Guid maintenanceRecordId, RecordMaintenanceCommand cmd)
        {
            cmd = cmd with { MaintenanceRecordId = maintenanceRecordId };
            await _mediator.Send(cmd);
            return NoContent();
        }

        /// <summary>
        /// Gets checkout/assignment history for an asset.
        /// </summary>
        [HttpGet("assets/{assetId:guid}/checkouts")]
        public async Task<IActionResult> GetCheckoutHistory(Guid assetId)
        {
            var result = await _mediator.Send(new GetAssetCheckoutHistoryQuery(assetId));
            return Ok(result);
        }

        /// <summary>
        /// Gets full change history for an asset.
        /// </summary>
        [HttpGet("assets/{assetId:guid}/history")]
        public async Task<IActionResult> GetHistory(Guid assetId)
        {
            var result = await _mediator.Send(new GetAssetHistoryQuery(assetId));
            return Ok(result);
        }

        /// <summary>
        /// Searches assets across all projects by name, tag, serial number, or manufacturer.
        /// </summary>
        [HttpGet("assets/search")]
        public async Task<IActionResult> Search(
            [FromQuery] string q,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest("Search term is required.");

            var result = await _mediator.Send(new SearchAssetsQuery(q, page, pageSize));
            return Ok(result);
        }

        /// <summary>
        /// Returns the available asset types and defaults for a given domain type.
        /// Use this when creating assets within a domain project to populate type dropdowns
        /// and auto-apply default depreciation, maintenance intervals, and compliance notes.
        /// </summary>
        [HttpGet("assets/domain-config/{domainType}")]
        public async Task<IActionResult> GetDomainAssetConfig(DomainType domainType)
        {
            var result = await _mediator.Send(new GetDomainAssetConfigQuery(domainType));
            return Ok(result);
        }
    }
}
