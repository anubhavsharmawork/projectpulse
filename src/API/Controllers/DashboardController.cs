using Application.Budget.Queries;
using Application.Dashboard.Queries;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;

namespace API.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize(Policy = "MemberPolicy")]
    public class DashboardController : ControllerBase
    {
        private readonly IMediator _mediator;
        public DashboardController(IMediator mediator) => _mediator = mediator;

        [HttpGet("metrics")]
        public async Task<IActionResult> GetMetrics([FromQuery] string? domainType)
        {
            DomainType? parsed = null;
            if (!string.IsNullOrWhiteSpace(domainType) && Enum.TryParse<DomainType>(domainType, true, out var dt))
                parsed = dt;
            return Ok(await _mediator.Send(new GetMetricsQuery(parsed)));
        }

        [HttpGet("budget")]
        [Authorize(Policy = "AdminPolicy")]
        public async Task<IActionResult> GetBudgetStatus()
            => Ok(await _mediator.Send(new GetBudgetStatusQuery()));
    }
}
