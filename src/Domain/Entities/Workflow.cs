using Domain.Enums;

namespace Domain.Entities
{
    public class Workflow : BaseEntity
    {
        public Guid TenantId { get; set; }
        public string Name { get; set; } = string.Empty;
        public DomainType DomainType { get; set; }
        public List<WorkflowState> States { get; set; } = new();
    }
}
