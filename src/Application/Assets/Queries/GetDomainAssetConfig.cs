using Application.Common.Interfaces;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Assets.Queries
{
    public record GetDomainAssetConfigQuery(DomainType DomainType) : IRequest<DomainAssetConfigResult>;

    public record DomainAssetConfigItemDto(
        Guid Id,
        AssetType AssetType,
        AssetCategory Category,
        string DisplayLabel,
        string? Description,
        DepreciationMethod DefaultDepreciationMethod,
        int DefaultUsefulLifeYears,
        int? DefaultMaintenanceIntervalDays,
        string? ComplianceNotes,
        int SortOrder);

    public record DomainAssetConfigResult(
        DomainType DomainType,
        List<DomainAssetConfigItemDto> AssetTypes);

    public class GetDomainAssetConfigHandler : IRequestHandler<GetDomainAssetConfigQuery, DomainAssetConfigResult>
    {
        private readonly IAppDbContext _db;

        public GetDomainAssetConfigHandler(IAppDbContext db) => _db = db;

        public async Task<DomainAssetConfigResult> Handle(GetDomainAssetConfigQuery request, CancellationToken cancellationToken)
        {
            var items = await _db.DomainAssetConfigs
                .AsNoTracking()
                .Where(c => c.DomainType == request.DomainType && c.IsActive)
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.DisplayLabel)
                .Select(c => new DomainAssetConfigItemDto(
                    c.Id,
                    c.AssetType,
                    c.Category,
                    c.DisplayLabel,
                    c.Description,
                    c.DefaultDepreciationMethod,
                    c.DefaultUsefulLifeYears,
                    c.DefaultMaintenanceIntervalDays,
                    c.ComplianceNotes,
                    c.SortOrder))
                .ToListAsync(cancellationToken);

            return new DomainAssetConfigResult(request.DomainType, items);
        }
    }
}
