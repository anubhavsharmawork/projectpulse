namespace Domain.Entities
{
    public class AssetCheckout : BaseEntity
    {
        public Guid TenantId { get; set; }
        public Guid AssetId { get; set; }
        public Asset Asset { get; set; } = null!;

        public Guid CheckedOutToUserId { get; set; }
        public User CheckedOutToUser { get; set; } = null!;

        public DateTime CheckedOutAt { get; set; } = DateTime.UtcNow;
        public DateTime? ExpectedReturnDate { get; set; }
        public DateTime? ActualReturnDate { get; set; }
        public string CheckedOutBy { get; set; } = string.Empty;
        public string? CheckedInBy { get; set; }
        public string Condition { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }
}
