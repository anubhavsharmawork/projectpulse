using Domain.Enums;

namespace Domain.Entities
{
    public class AssetHistoryEntry : BaseEntity
    {
        public Guid TenantId { get; set; }
        public Guid AssetId { get; set; }
        public Asset Asset { get; set; } = null!;

        public AssetChangeType ChangeType { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public string ChangedBy { get; set; } = string.Empty;
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
        public string? Reason { get; set; }
    }
}
