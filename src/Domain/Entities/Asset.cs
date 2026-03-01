using Domain.Enums;

namespace Domain.Entities
{
    public class Asset : BaseEntity
    {
        public Guid TenantId { get; set; }
        public Guid ProjectId { get; set; }
        public string AssetTag { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime PurchaseDate { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal CurrentValue { get; set; }
        public AssetStatus Status { get; set; } = AssetStatus.Available;
        public string Location { get; set; } = string.Empty;
        public Guid? AssignedToUserId { get; set; }
        public string? SerialNumber { get; set; }
        public string? Manufacturer { get; set; }
        public string? Model { get; set; }
        public DateTime? WarrantyExpiryDate { get; set; }
        public string? Notes { get; set; }
        public DepreciationMethod DepreciationMethod { get; set; } = DepreciationMethod.StraightLine;
        public int UsefulLifeYears { get; set; } = 5;

        public AssetType Type { get; set; }
        public AssetCategory Category { get; set; } = AssetCategory.Physical;

        // Physical asset fields (formerly PhysicalAsset subclass)
        public decimal? Weight { get; set; }
        public string? Dimensions { get; set; }
        public string? BarcodeValue { get; set; }
        public int? MaintenanceIntervalDays { get; set; }
        public DateTime? LastMaintenanceDate { get; set; }
        public DateTime? NextMaintenanceDate { get; set; }

        // Digital / license asset fields
        public string? LicenseKey { get; set; }
        public int? LicensedSeats { get; set; }
        public DateTime? LicenseExpiryDate { get; set; }
        public string? Vendor { get; set; }

        // Infrastructure asset fields (grid, utility, civil)
        public string? GridReference { get; set; }
        public string? Capacity { get; set; }
        public string? RegulatoryId { get; set; }

        // Domain asset config link for auto-defaults
        public Guid? DomainAssetConfigId { get; set; }
        public DomainAssetConfig? DomainAssetConfig { get; set; }

        // Navigation properties
        public Project Project { get; set; } = null!;
        public User? AssignedToUser { get; set; }
        public List<MaintenanceRecord> MaintenanceRecords { get; set; } = new();
        public List<AssetHistoryEntry> AssetHistory { get; set; } = new();
        public List<AssetCheckout> AssetCheckouts { get; set; } = new();
    }
}
