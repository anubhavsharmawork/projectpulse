namespace Domain.Entities
{
    public class MentionNotification
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid CommentId { get; set; }
        public Guid WorkItemId { get; set; }
        public Guid MentionedByUserId { get; set; }
        public string CommentBody { get; set; } = string.Empty;
        public string WorkItemTitle { get; set; } = string.Empty;
        public string MentionedByName { get; set; } = string.Empty;
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
