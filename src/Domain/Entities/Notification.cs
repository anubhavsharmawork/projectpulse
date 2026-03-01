namespace Domain.Entities
{
    public enum NotificationType
    {
        StateTransition = 1,
        AssignmentChange = 2,
        OverdueTask = 3,
        Mention = 4,
        Comment = 5,
        General = 6
    }

    public class Notification
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid UserId { get; set; }
        public NotificationType Type { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Optional FK to related entity (work item, comment, etc.)
        /// </summary>
        public Guid? RelatedEntityId { get; set; }
    }
}
