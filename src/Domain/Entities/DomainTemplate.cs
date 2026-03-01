using Domain.Enums;

namespace Domain.Entities
{
    public class DomainTemplate : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public DomainType DomainType { get; set; }

        /// <summary>
        /// JSON-serialized default notification rules.
        /// </summary>
        public string? DefaultNotificationRules { get; set; }

        /// <summary>
        /// JSON-serialized mapping of WorkItemType int values to domain-specific labels.
        /// E.g. {"1":"Phase","2":"Activity","3":"Punch Item","4":"SubItem"}
        /// </summary>
        public string? WorkItemTypeLabels { get; set; }

        /// <summary>
        /// The default workflow associated with this template.
        /// </summary>
        public Guid? DefaultWorkflowId { get; set; }
        public Workflow? DefaultWorkflow { get; set; }

        /// <summary>
        /// Custom fields bundled with this template.
        /// </summary>
        public List<CustomField> CustomFields { get; set; } = new();
    }
}
