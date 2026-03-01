using Application.CustomFields.Commands;
using Application.CustomFields.Queries;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/custom-fields")]
    [Authorize(Policy = "MemberPolicy")]
    public class CustomFieldsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<CustomFieldsController> _logger;

        public CustomFieldsController(IMediator mediator, ILogger<CustomFieldsController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        /// <summary>
        /// Get custom field definitions for a domain type, optionally filtered by entity type (work item level).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetByDomain([FromQuery] string domainType, [FromQuery] string? entityType = null)
        {
            try
            {
                _logger.LogInformation("Fetching custom fields for domain: {DomainType}, entityType: {EntityType}", domainType, entityType);
                var result = await _mediator.Send(new GetCustomFieldsByDomainQuery(domainType, entityType));
                _logger.LogInformation("Returning {Count} custom fields for domain: {DomainType}", result.Count, domainType);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch custom fields for domain: {DomainType}", domainType);
                return StatusCode(500, new { error = "Failed to load custom fields.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Get custom field values for a specific entity (work item, project, etc.).
        /// </summary>
        [HttpGet("values/{entityId:guid}")]
        public async Task<IActionResult> GetValuesForEntity(Guid entityId)
        {
            try
            {
                var result = await _mediator.Send(new GetCustomFieldValuesForEntityQuery(entityId));
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch custom field values for entity: {EntityId}", entityId);
                return StatusCode(500, new { error = "Failed to load custom field values.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Save (create or update) a custom field value for an entity.
        /// </summary>
        [HttpPost("values")]
        public async Task<IActionResult> SaveValue([FromBody] SaveCustomFieldValueCommand cmd)
        {
            try
            {
                var id = await _mediator.Send(cmd);
                return Ok(new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save custom field value.");
                return StatusCode(500, new { error = "Failed to save custom field value.", detail = ex.Message });
            }
        }
    }
}
