using Domain.Enums;

namespace Domain.Entities
{
    public class MaintenanceRecord : BaseEntity
    {
        public Guid TenantId { get; set; }
        public Guid AssetId { get; set; }
        public Asset Asset { get; set; } = null!;

        public DateTime ScheduledDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public MaintenanceType MaintenanceType { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? PerformedBy { get; set; }
        public decimal Cost { get; set; }
        public string? Notes { get; set; }
        public DateTime? NextMaintenanceDate { get; set; }
    }
}
