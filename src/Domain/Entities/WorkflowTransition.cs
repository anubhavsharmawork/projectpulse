namespace Domain.Entities
{
    public class WorkflowTransition : BaseEntity
    {
        public Guid TenantId { get; set; }
        public Guid WorkItemId { get; set; }
        public WorkItem WorkItem { get; set; } = null!;

        public Guid FromStateId { get; set; }
        public WorkflowState FromState { get; set; } = null!;

        public Guid ToStateId { get; set; }
        public WorkflowState ToState { get; set; } = null!;

        public Guid TransitionedByUserId { get; set; }
        public User TransitionedByUser { get; set; } = null!;

        public string? Comment { get; set; }
    }
}
