using Application.Common.Interfaces;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Legal.Queries;

public record GetLegalDocumentQuery(LegalDocumentType DocumentType) : IRequest<LegalDocumentDto?>;

public record LegalDocumentDto(
    Guid Id,
    string DocumentType,
    string Version,
    DateTime EffectiveDate,
    string Content);

public class GetLegalDocumentHandler : IRequestHandler<GetLegalDocumentQuery, LegalDocumentDto?>
{
    private readonly IAppDbContext _db;

    public GetLegalDocumentHandler(IAppDbContext db) => _db = db;

    public async Task<LegalDocumentDto?> Handle(GetLegalDocumentQuery request, CancellationToken cancellationToken)
    {
        var doc = await _db.LegalDocuments
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.DocumentType == request.DocumentType && d.IsActive, cancellationToken);

        if (doc is null) return null;

        return new LegalDocumentDto(
            doc.Id,
            doc.DocumentType.ToString(),
            doc.Version,
            doc.EffectiveDate,
            doc.Content);
    }
}
