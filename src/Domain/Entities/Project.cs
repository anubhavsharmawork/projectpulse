using Domain.Enums;

namespace Domain.Entities
{
    public class Project : BaseEntity
    {
        public Guid TenantId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid OwnerId { get; set; }
        public bool IsPublic { get; set; }

        // Domain-agnostic PM fields
        public DomainType DomainType { get; set; }
        public Guid? TemplateId { get; set; }
        public DomainTemplate? Template { get; set; }
        public Guid? WorkflowId { get; set; }
        public Workflow? Workflow { get; set; }

        // Budget tracking
        public decimal EstimatedCost { get; set; }
        public decimal ActualCost { get; set; }

        public List<WorkItem> WorkItems { get; set; } = new();
        public List<Team> Teams { get; set; } = new();
    }

    public class Comment
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid WorkItemId { get; set; }
        public Guid AuthorId { get; set; }
        public string Body { get; set; } = string.Empty;
        public List<Guid> MentionedUserIds { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
    }
}
