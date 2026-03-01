using Application.Common.Interfaces;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace API.Middleware;

/// <summary>
/// Blocks authenticated API requests from users who haven't accepted the current
/// active Terms of Service and Privacy Policy versions.
/// Exempts: anonymous endpoints, auth endpoints, legal endpoints (so users can fetch docs and accept).
/// </summary>
public class LegalAcceptanceMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<LegalAcceptanceMiddleware> _logger;

    private static readonly HashSet<string> ExemptPathSegments = new(StringComparer.OrdinalIgnoreCase)
    {
        "/auth/",
        "/legal/",
        "/health/",
        "/hubs/"
    };

    public LegalAcceptanceMiddleware(RequestDelegate next, ILogger<LegalAcceptanceMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IAppDbContext db)
    {
        var path = context.Request.Path.Value ?? "";

        // Skip non-API paths, exempt endpoints, and unauthenticated requests
        if (!path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase)
            || ExemptPathSegments.Any(seg => path.Contains(seg, StringComparison.OrdinalIgnoreCase))
            || context.User.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            await _next(context);
            return;
        }

        // Check if there are active legal documents
        var activeTerms = await db.LegalDocuments.AsNoTracking()
            .FirstOrDefaultAsync(d => d.DocumentType == LegalDocumentType.TermsOfService && d.IsActive);
        var activePrivacy = await db.LegalDocuments.AsNoTracking()
            .FirstOrDefaultAsync(d => d.DocumentType == LegalDocumentType.PrivacyPolicy && d.IsActive);

        // If no legal documents exist, skip enforcement
        if (activeTerms is null && activePrivacy is null)
        {
            await _next(context);
            return;
        }

        var user = await db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.TermsVersion, u.PrivacyVersion })
            .FirstOrDefaultAsync();

        if (user is null)
        {
            await _next(context);
            return;
        }

        var termsOk = activeTerms is null || user.TermsVersion == activeTerms.Version;
        var privacyOk = activePrivacy is null || user.PrivacyVersion == activePrivacy.Version;

        if (!termsOk || !privacyOk)
        {
            _logger.LogWarning("User {UserId} blocked — legal acceptance required (terms: {TermsOk}, privacy: {PrivacyOk})",
                userId, termsOk, privacyOk);

            context.Response.StatusCode = StatusCodes.Status451UnavailableForLegalReasons;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Legal acceptance required",
                requiresTerms = !termsOk,
                currentTermsVersion = activeTerms?.Version,
                requiresPrivacy = !privacyOk,
                currentPrivacyVersion = activePrivacy?.Version
            });
            return;
        }

        await _next(context);
    }
}
