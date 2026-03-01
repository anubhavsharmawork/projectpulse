namespace Domain.Entities
{
    public class TeamMember : BaseEntity
    {
        public Guid TenantId { get; set; }
        public Guid TeamId { get; set; }
        public Team Team { get; set; } = null!;

        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public string Role { get; set; } = string.Empty;

        /// <summary>
        /// JSON-serialized list of domain expertise areas (MultiSelect).
        /// </summary>
        public string? DomainExpertise { get; set; }

        /// <summary>
        /// Comma-separated skill tags.
        /// </summary>
        public string? Skills { get; set; }

        /// <summary>
        /// Maximum hours available per week.
        /// </summary>
        public decimal AvailabilityHoursPerWeek { get; set; }

        /// <summary>
        /// Hourly cost rate in $ (USD).
        /// </summary>
        public decimal CostRate { get; set; }
    }
}
