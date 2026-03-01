using Application.Legal.Commands;
using Application.Legal.Queries;
using Asp.Versioning;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class LegalController : ControllerBase
{
    private readonly IMediator _mediator;

    public LegalController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Get the current active Terms of Service.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("terms")]
    public async Task<IActionResult> GetTerms()
    {
        var doc = await _mediator.Send(new GetLegalDocumentQuery(LegalDocumentType.TermsOfService));
        return doc is null ? NotFound() : Ok(doc);
    }

    /// <summary>
    /// Get the current active Privacy Policy.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("privacy")]
    public async Task<IActionResult> GetPrivacy()
    {
        var doc = await _mediator.Send(new GetLegalDocumentQuery(LegalDocumentType.PrivacyPolicy));
        return doc is null ? NotFound() : Ok(doc);
    }

    /// <summary>
    /// Get the current user's legal acceptance status.
    /// </summary>
    [Authorize(Policy = "MemberPolicy")]
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        var status = await _mediator.Send(new GetLegalStatusQuery());
        return Ok(status);
    }

    /// <summary>
    /// Record the current user's acceptance of Terms and Privacy Policy.
    /// </summary>
    [Authorize(Policy = "MemberPolicy")]
    [HttpPost("accept")]
    public async Task<IActionResult> Accept([FromBody] AcceptLegalRequest request)
    {
        try
        {
            await _mediator.Send(new AcceptLegalCommand(request.TermsVersion, request.PrivacyVersion));
            return Ok(new { accepted = true });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

public record AcceptLegalRequest(string TermsVersion, string PrivacyVersion);
