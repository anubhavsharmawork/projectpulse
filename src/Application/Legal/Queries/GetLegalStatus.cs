using Application.Common.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Application.Legal.Queries;

public record GetLegalStatusQuery : IRequest<LegalStatusDto>;

public record LegalStatusDto(
    bool TermsAccepted,
    string? AcceptedTermsVersion,
    string? CurrentTermsVersion,
    bool PrivacyAccepted,
    string? AcceptedPrivacyVersion,
    string? CurrentPrivacyVersion,
    bool RequiresAcceptance);

public class GetLegalStatusHandler : IRequestHandler<GetLegalStatusQuery, LegalStatusDto>
{
    private readonly IAppDbContext _db;
    private readonly IHttpContextAccessor _http;

    public GetLegalStatusHandler(IAppDbContext db, IHttpContextAccessor http)
    {
        _db = db;
        _http = http;
    }

    public async Task<LegalStatusDto> Handle(GetLegalStatusQuery request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var user = await _db.Users.IgnoreQueryFilters().AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        var currentTerms = await _db.LegalDocuments.AsNoTracking()
            .FirstOrDefaultAsync(d => d.DocumentType == Domain.Enums.LegalDocumentType.TermsOfService && d.IsActive, cancellationToken);
        var currentPrivacy = await _db.LegalDocuments.AsNoTracking()
            .FirstOrDefaultAsync(d => d.DocumentType == Domain.Enums.LegalDocumentType.PrivacyPolicy && d.IsActive, cancellationToken);

        var termsOk = currentTerms is null || user?.TermsVersion == currentTerms.Version;
        var privacyOk = currentPrivacy is null || user?.PrivacyVersion == currentPrivacy.Version;

        return new LegalStatusDto(
            TermsAccepted: termsOk,
            AcceptedTermsVersion: user?.TermsVersion,
            CurrentTermsVersion: currentTerms?.Version,
            PrivacyAccepted: privacyOk,
            AcceptedPrivacyVersion: user?.PrivacyVersion,
            CurrentPrivacyVersion: currentPrivacy?.Version,
            RequiresAcceptance: !termsOk || !privacyOk);
    }

    private Guid GetCurrentUserId()
    {
        var claim = _http.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var id) ? id : throw new UnauthorizedAccessException();
    }
}
