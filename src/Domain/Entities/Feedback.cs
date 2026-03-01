namespace Domain.Entities
{
    public class Feedback : BaseEntity
    {
        public Guid TenantId { get; set; }
        public Guid? UserId { get; set; }
        public string? UserEmail { get; set; }
        public string? UserDisplayName { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime? ProcessedAt { get; set; }
    }
}
