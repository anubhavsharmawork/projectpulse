namespace Domain.Entities
{
    public class Attachment : BaseEntity
    {
        public Guid TenantId { get; set; }
        public Guid WorkItemId { get; set; }
        public WorkItem WorkItem { get; set; } = null!;

        public string FileName { get; set; } = string.Empty;
        public string StorageUrl { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
    }
}
