using Application.Audit.Queries;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/audit-logs")]
    [Authorize(Policy = "AdminPolicy")]
    public class AuditController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AuditController(IMediator mediator) => _mediator = mediator;

        [HttpGet]
        public async Task<IActionResult> GetLogs(
            [FromQuery] string? entityType,
            [FromQuery] Guid? entityId,
            [FromQuery] Guid? userId,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] int limit = 100)
            => Ok(await _mediator.Send(new GetAuditLogsQuery(entityType, entityId, userId, from, to, limit)));
    }
}
