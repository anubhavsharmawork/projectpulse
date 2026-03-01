using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Assets.Queries
{
    public record GetAssetByIdQuery(Guid AssetId) : IRequest<AssetDetailDto?>;

    public record AssetDetailDto(
        Guid Id,
        Guid ProjectId,
        string AssetTag,
        string Name,
        string? Description,
        DateTime PurchaseDate,
        decimal PurchasePrice,
        decimal CurrentValue,
        AssetStatus Status,
        string Location,
        Guid? AssignedToUserId,
        string? AssignedToUserName,
        string? SerialNumber,
        string? Manufacturer,
        string? Model,
        DateTime? WarrantyExpiryDate,
        string? Notes,
        DepreciationMethod DepreciationMethod,
        int UsefulLifeYears,
        AssetType Type,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        string CreatedBy,
        bool IsActive,
        decimal? Weight,
        string? Dimensions,
        string? BarcodeValue,
        int? MaintenanceIntervalDays,
        DateTime? LastMaintenanceDate,
        DateTime? NextMaintenanceDate);

    public class GetAssetByIdHandler : IRequestHandler<GetAssetByIdQuery, AssetDetailDto?>
    {
        private readonly IAppDbContext _db;

        public GetAssetByIdHandler(IAppDbContext db) => _db = db;

        public async Task<AssetDetailDto?> Handle(GetAssetByIdQuery request, CancellationToken cancellationToken)
        {
            var asset = await _db.Assets
                .AsNoTracking()
                .Include(a => a.AssignedToUser)
                .FirstOrDefaultAsync(a => a.Id == request.AssetId && a.IsActive, cancellationToken);

            if (asset is null)
                return null;

            return new AssetDetailDto(
                asset.Id,
                asset.ProjectId,
                asset.AssetTag,
                asset.Name,
                asset.Description,
                asset.PurchaseDate,
                asset.PurchasePrice,
                asset.CurrentValue,
                asset.Status,
                asset.Location,
                asset.AssignedToUserId,
                asset.AssignedToUser?.DisplayName,
                asset.SerialNumber,
                asset.Manufacturer,
                asset.Model,
                asset.WarrantyExpiryDate,
                asset.Notes,
                asset.DepreciationMethod,
                asset.UsefulLifeYears,
                asset.Type,
                asset.CreatedAt,
                asset.UpdatedAt,
                asset.CreatedBy,
                asset.IsActive,
                asset.Weight,
                asset.Dimensions,
                asset.BarcodeValue,
                asset.MaintenanceIntervalDays,
                asset.LastMaintenanceDate,
                asset.NextMaintenanceDate);
        }
    }
}
