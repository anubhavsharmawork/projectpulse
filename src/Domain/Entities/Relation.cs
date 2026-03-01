using Domain.Enums;

namespace Domain.Entities
{
    public class Relation : BaseEntity
    {
        public Guid TenantId { get; set; }
        public Guid SourceWorkItemId { get; set; }
        public WorkItem SourceWorkItem { get; set; } = null!;

        public Guid TargetWorkItemId { get; set; }
        public WorkItem TargetWorkItem { get; set; } = null!;

        public RelationType RelationType { get; set; }
    }
}
