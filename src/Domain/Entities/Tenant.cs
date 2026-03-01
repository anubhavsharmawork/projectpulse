using Domain.Enums;

namespace Domain.Entities
{
    public class Tenant
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Subdomain { get; set; } = string.Empty;
        public TenantTier Tier { get; set; } = TenantTier.Starter;
        public int MaxUsers { get; set; } = 5;
        public int MaxProjects { get; set; } = 10;
        public long MaxStorageBytes { get; set; } = 10L * 1024 * 1024 * 1024; // 10 GB
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? SubscriptionExpiresAt { get; set; }

        /// <summary>
        /// JSON column for flexible tenant-specific configuration (branding, feature flags, etc.).
        /// </summary>
        public string? Settings { get; set; }
    }
}
