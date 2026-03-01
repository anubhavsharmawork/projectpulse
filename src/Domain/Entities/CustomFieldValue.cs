namespace Domain.Entities
{
    public class CustomFieldValue : BaseEntity
    {
        public Guid TenantId { get; set; }
        public Guid CustomFieldId { get; set; }
        public CustomField CustomField { get; set; } = null!;

        /// <summary>
        /// The entity this value belongs to (WorkItem, Project, etc.).
        /// </summary>
        public Guid EntityId { get; set; }

        /// <summary>
        /// The type name of the entity (e.g., "WorkItem", "Project").
        /// </summary>
        public string EntityType { get; set; } = string.Empty;

        /// <summary>
        /// The stored value serialized as a string.
        /// </summary>
        public string? Value { get; set; }
    }
}
