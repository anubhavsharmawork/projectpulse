using Domain.Attributes;

namespace Domain.Entities
{
    public enum Role
    {
        Member = 0,
        Admin = 1
    }

    public class User
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        [Encrypted]
        public string Email { get; set; } = string.Empty;

        [Encrypted]
        public string PasswordHash { get; set; } = string.Empty; // placeholder for identity provider

        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Unique username derived from email on registration (part before @).
        /// Used for team assignment instead of exposing user IDs or emails.
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        public Role Role { get; set; } = Role.Member;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // ── Timezone ──

        /// <summary>
        /// IANA timezone identifier (e.g., "Pacific/Auckland", "America/New_York").
        /// Auto-detected on first login; user can override via settings.
        /// </summary>
        public string? TimeZoneId { get; set; }

        /// <summary>
        /// UTC offset in minutes (e.g., +720 for NZST, -300 for EST).
        /// Stored alongside TimeZoneId for quick display calculations.
        /// </summary>
        public int? TimeZoneOffset { get; set; }

        // ── Legal acceptance ──

        /// <summary>
        /// UTC timestamp when the user last accepted the Terms of Service.
        /// </summary>
        public DateTime? TermsAcceptedAt { get; set; }

        /// <summary>
        /// Version string of the Terms of Service the user accepted (e.g., "1.0").
        /// </summary>
        public string? TermsVersion { get; set; }

        /// <summary>
        /// UTC timestamp when the user last accepted the Privacy Policy.
        /// </summary>
        public DateTime? PrivacyAcceptedAt { get; set; }

        /// <summary>
        /// Version string of the Privacy Policy the user accepted (e.g., "1.0").
        /// </summary>
        public string? PrivacyVersion { get; set; }

        /// <summary>
        /// IP address from which the user accepted the legal documents.
        /// Stored for legal compliance audit trail.
        /// </summary>
        public string? LegalAcceptanceIp { get; set; }

        /// <summary>
        /// FK to the granular RBAC role. Nullable for backward compatibility.
        /// </summary>
        public Guid? AppRoleId { get; set; }
        public AppRole? AppRole { get; set; }
    }
}
