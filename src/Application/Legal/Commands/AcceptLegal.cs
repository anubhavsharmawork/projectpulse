using Application.Common.Interfaces;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Application.Legal.Commands;

public record AcceptLegalCommand(string TermsVersion, string PrivacyVersion) : IRequest<Unit>;

public class AcceptLegalHandler : IRequestHandler<AcceptLegalCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly IHttpContextAccessor _http;

    public AcceptLegalHandler(IAppDbContext db, IHttpContextAccessor http)
    {
        _db = db;
        _http = http;
    }

    public async Task<Unit> Handle(AcceptLegalCommand request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var user = await _db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new InvalidOperationException("User not found");

        // Verify versions are active
        var activeTerms = await _db.LegalDocuments
            .FirstOrDefaultAsync(d => d.DocumentType == LegalDocumentType.TermsOfService && d.IsActive, cancellationToken);
        var activePrivacy = await _db.LegalDocuments
            .FirstOrDefaultAsync(d => d.DocumentType == LegalDocumentType.PrivacyPolicy && d.IsActive, cancellationToken);

        if (activeTerms is not null && activeTerms.Version != request.TermsVersion)
            throw new InvalidOperationException($"Terms version '{request.TermsVersion}' is not the current active version.");
        if (activePrivacy is not null && activePrivacy.Version != request.PrivacyVersion)
            throw new InvalidOperationException($"Privacy version '{request.PrivacyVersion}' is not the current active version.");

        var now = DateTime.UtcNow;
        var ip = _http.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "unknown";

        user.TermsAcceptedAt = now;
        user.TermsVersion = request.TermsVersion;
        user.PrivacyAcceptedAt = now;
        user.PrivacyVersion = request.PrivacyVersion;
        user.LegalAcceptanceIp = ip;

        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }

    private Guid GetCurrentUserId()
    {
        var claim = _http.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var id) ? id : throw new UnauthorizedAccessException();
    }
}
