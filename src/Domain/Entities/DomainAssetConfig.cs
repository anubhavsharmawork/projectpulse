using Domain.Enums;

namespace Domain.Entities
{
    /// <summary>
    /// Data-driven catalog that maps each DomainType to its available asset types
    /// with sensible defaults for depreciation, useful life, maintenance, and compliance.
    /// Seeded per domain so that creating assets in a Healthcare project automatically
    /// surfaces MedicalDevice, ImagingEquipment, etc., while an Infrastructure project
    /// surfaces Transformer, SmartMeter, Pipeline, and so on.
    /// </summary>
    public class DomainAssetConfig : BaseEntity
    {
        public DomainType DomainType { get; set; }
        public AssetType AssetType { get; set; }
        public AssetCategory Category { get; set; }

        /// <summary>
        /// Human-readable label for this asset type within the domain context.
        /// E.g. "Smart Meter" for Infrastructure, "Body Camera" for PublicSafety.
        /// </summary>
        public string DisplayLabel { get; set; } = string.Empty;

        public string? Description { get; set; }
        public DepreciationMethod DefaultDepreciationMethod { get; set; } = DepreciationMethod.StraightLine;
        public int DefaultUsefulLifeYears { get; set; } = 5;
        public int? DefaultMaintenanceIntervalDays { get; set; }

        /// <summary>
        /// Regulatory/compliance notes relevant to this asset type in this domain.
        /// E.g. "Requires annual calibration per IEC 62052" for smart meters.
        /// </summary>
        public string? ComplianceNotes { get; set; }

        /// <summary>
        /// JSON-serialized list of additional field names that should be collected
        /// when creating this asset type. Integrates with the CustomField system.
        /// </summary>
        public string? DefaultFields { get; set; }

        /// <summary>
        /// Display ordering within the domain's asset catalog.
        /// </summary>
        public int SortOrder { get; set; }
    }
}
