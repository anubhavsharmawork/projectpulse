using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Application.Assets.Commands
{
    public record CreateAssetCommand(
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
        string? SerialNumber,
        string? Manufacturer,
        string? Model,
        DateTime? WarrantyExpiryDate,
        string? Notes,
        DepreciationMethod DepreciationMethod,
        int UsefulLifeYears,
        AssetType AssetType,
        AssetCategory Category,
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
        string? RegulatoryId,
        Guid? DomainAssetConfigId) : IRequest<CreateAssetResult>;

    public record CreateAssetResult(Guid AssetId);

    public class CreateAssetHandler : IRequestHandler<CreateAssetCommand, CreateAssetResult>
    {
        private readonly IAppDbContext _db;
        private readonly IHttpContextAccessor _http;

        public CreateAssetHandler(IAppDbContext db, IHttpContextAccessor http)
        {
            _db = db;
            _http = http;
        }

        public async Task<CreateAssetResult> Handle(CreateAssetCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("Asset name is required.");

            if (string.IsNullOrWhiteSpace(request.AssetTag))
                throw new ArgumentException("Asset tag is required.");

            var tagExists = await _db.Assets
                .AnyAsync(a => a.AssetTag == request.AssetTag, cancellationToken);
            if (tagExists)
                throw new InvalidOperationException($"Asset tag '{request.AssetTag}' already exists.");

            var projectExists = await _db.Projects
                .AnyAsync(p => p.Id == request.ProjectId && p.IsActive, cancellationToken);
            if (!projectExists)
                throw new ArgumentException("Invalid project ID.");

            if (request.PurchasePrice < 0)
                throw new ArgumentException("Purchase price must be zero or greater.");

            var userIdClaim = _http.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var createdBy = userIdClaim ?? string.Empty;

            var entity = new Asset
            {
                Id = Guid.NewGuid(),
                ProjectId = request.ProjectId,
                AssetTag = request.AssetTag,
                Name = request.Name,
                Description = request.Description,
                PurchaseDate = request.PurchaseDate,
                PurchasePrice = request.PurchasePrice,
                CurrentValue = request.CurrentValue,
                Status = request.Status,
                Location = request.Location,
                AssignedToUserId = request.AssignedToUserId,
                SerialNumber = request.SerialNumber,
                Manufacturer = request.Manufacturer,
                Model = request.Model,
                WarrantyExpiryDate = request.WarrantyExpiryDate,
                Notes = request.Notes,
                DepreciationMethod = request.DepreciationMethod,
                UsefulLifeYears = request.UsefulLifeYears,
                Type = request.AssetType,
                Category = request.Category,
                CreatedBy = createdBy,
                Weight = request.Weight,
                Dimensions = request.Dimensions,
                BarcodeValue = request.BarcodeValue,
                MaintenanceIntervalDays = request.MaintenanceIntervalDays,
                LicenseKey = request.LicenseKey,
                LicensedSeats = request.LicensedSeats,
                LicenseExpiryDate = request.LicenseExpiryDate,
                Vendor = request.Vendor,
                GridReference = request.GridReference,
                Capacity = request.Capacity,
                RegulatoryId = request.RegulatoryId,
                DomainAssetConfigId = request.DomainAssetConfigId
            };

            _db.Assets.Add(entity);

            _db.AssetHistoryEntries.Add(new AssetHistoryEntry
            {
                Id = Guid.NewGuid(),
                AssetId = entity.Id,
                ChangeType = AssetChangeType.Created,
                NewValue = entity.Name,
                ChangedBy = createdBy,
                ChangedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync(cancellationToken);
            return new CreateAssetResult(entity.Id);
        }
    }
}
