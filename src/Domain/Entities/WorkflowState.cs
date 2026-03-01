namespace Domain.Entities
{
    public class WorkflowState : BaseEntity
    {
        public Guid TenantId { get; set; }
        public Guid WorkflowId { get; set; }
        public Workflow Workflow { get; set; } = null!;

        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Order in which this state appears in the workflow.
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// Hex color code for UI rendering (e.g., "#3B82F6").
        /// </summary>
        public string Color { get; set; } = "#6B7280";

        /// <summary>
        /// Indicates whether this is the initial state of the workflow.
        /// </summary>
        public bool IsInitial { get; set; }

        /// <summary>
        /// Indicates whether this is a terminal/done state.
        /// </summary>
        public bool IsFinal { get; set; }

        /// <summary>
        /// JSON array of WorkflowState IDs that this state can transition to.
        /// </summary>
        public string? AllowedTransitions { get; set; }

        /// <summary>
        /// JSON array of custom field names that must be filled before entering this state.
        /// </summary>
        public string? RequiredFields { get; set; }

        /// <summary>
        /// Whether to fire an auto-notification when a work item enters this state.
        /// </summary>
        public bool NotifyOnEntry { get; set; }
    }
}
