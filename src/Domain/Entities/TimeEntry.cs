namespace Domain.Entities
{
    public class TimeEntry : BaseEntity
    {
        public Guid TenantId { get; set; }
        public Guid WorkItemId { get; set; }
        public WorkItem WorkItem { get; set; } = null!;

        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        /// <summary>
        /// Hours logged for this time entry.
        /// </summary>
        public decimal Hours { get; set; }

        public string? Description { get; set; }
        public DateTime LoggedDate { get; set; }

        /// <summary>
        /// Whether this time entry is billable to the client.
        /// </summary>
        public bool IsBillable { get; set; }
    }
}
