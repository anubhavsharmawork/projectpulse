using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Application.Assets.Commands
{
    public record UpdateAssetCommand(
        Guid AssetId,
        string Name,
        string? Description,
        AssetStatus Status,
        string Location,
        Guid? AssignedToUserId,
        string? SerialNumber,
        string? Manufacturer,
        string? Model,
        DateTime? WarrantyExpiryDate,
        string? Notes,
        decimal CurrentValue,
        DepreciationMethod DepreciationMethod,
        int UsefulLifeYears,
        decimal? Weight,
        string? Dimensions,
        string? BarcodeValue,
        int? MaintenanceIntervalDays,
        string? LicenseKey,
        int? LicensedSeats,
        DateTime? LicenseExpiryDate,
        string? Vendor,
        string? GridReference,
        string? Capacity,
        string? RegulatoryId) : IRequest<Unit>;

    public class UpdateAssetHandler : IRequestHandler<UpdateAssetCommand, Unit>
    {
        private readonly IAppDbContext _db;
        private readonly IHttpContextAccessor _http;

        public UpdateAssetHandler(IAppDbContext db, IHttpContextAccessor http)
        {
            _db = db;
            _http = http;
        }

        public async Task<Unit> Handle(UpdateAssetCommand request, CancellationToken cancellationToken)
        {
            var asset = await _db.Assets
                .FirstOrDefaultAsync(a => a.Id == request.AssetId && a.IsActive, cancellationToken)
                ?? throw new KeyNotFoundException("Asset not found.");

            var userIdClaim = _http.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var changedBy = userIdClaim ?? string.Empty;

            if (asset.Status != request.Status)
            {
                _db.AssetHistoryEntries.Add(new AssetHistoryEntry
                {
                    Id = Guid.NewGuid(),
                    AssetId = asset.Id,
                    ChangeType = AssetChangeType.StatusChanged,
                    OldValue = asset.Status.ToString(),
                    NewValue = request.Status.ToString(),
                    ChangedBy = changedBy,
                    ChangedAt = DateTime.UtcNow
                });
            }

            if (asset.Location != request.Location)
            {
                _db.AssetHistoryEntries.Add(new AssetHistoryEntry
                {
                    Id = Guid.NewGuid(),
                    AssetId = asset.Id,
                    ChangeType = AssetChangeType.LocationMoved,
                    OldValue = asset.Location,
                    NewValue = request.Location,
                    ChangedBy = changedBy,
                    ChangedAt = DateTime.UtcNow
                });
            }

            if (asset.CurrentValue != request.CurrentValue)
            {
                _db.AssetHistoryEntries.Add(new AssetHistoryEntry
                {
                    Id = Guid.NewGuid(),
                    AssetId = asset.Id,
                    ChangeType = AssetChangeType.ValueAdjusted,
                    OldValue = asset.CurrentValue.ToString("F2"),
                    NewValue = request.CurrentValue.ToString("F2"),
                    ChangedBy = changedBy,
                    ChangedAt = DateTime.UtcNow
                });
            }

            asset.Name = request.Name;
            asset.Description = request.Description;
            asset.Status = request.Status;
            asset.Location = request.Location;
            asset.AssignedToUserId = request.AssignedToUserId;
            asset.SerialNumber = request.SerialNumber;
            asset.Manufacturer = request.Manufacturer;
            asset.Model = request.Model;
            asset.WarrantyExpiryDate = request.WarrantyExpiryDate;
            asset.Notes = request.Notes;
            asset.CurrentValue = request.CurrentValue;
            asset.DepreciationMethod = request.DepreciationMethod;
            asset.UsefulLifeYears = request.UsefulLifeYears;
            asset.UpdatedAt = DateTime.UtcNow;

            asset.Weight = request.Weight;
            asset.Dimensions = request.Dimensions;
            asset.BarcodeValue = request.BarcodeValue;
            asset.MaintenanceIntervalDays = request.MaintenanceIntervalDays;
            asset.LicenseKey = request.LicenseKey;
            asset.LicensedSeats = request.LicensedSeats;
            asset.LicenseExpiryDate = request.LicenseExpiryDate;
            asset.Vendor = request.Vendor;
            asset.GridReference = request.GridReference;
            asset.Capacity = request.Capacity;
            asset.RegulatoryId = request.RegulatoryId;

            await _db.SaveChangesAsync(cancellationToken);
            return Unit.Value;
        }
    }
}
