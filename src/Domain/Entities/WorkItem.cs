namespace Domain.Entities
{
    public enum WorkItemType
    {
        Epic = 1,
        UserStory = 2,
        Task = 3,
        SubTask = 4,
        Bug = 5
    }

    public abstract class WorkItem
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid ProjectId { get; set; }
        public Guid? ParentId { get; set; }
        public WorkItem? Parent { get; set; }
        public List<WorkItem> Children { get; set; } = new();
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? AttachmentUrl { get; set; }
        public bool IsCompleted { get; set; }
        public Guid? AssigneeId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string CreatedBy { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime? CompletedAt { get; set; }
        public WorkItemType Type { get; protected set; }

        // Workflow state tracking
        public Guid? CurrentStateId { get; set; }
        public WorkflowState? CurrentState { get; set; }

        // Budget tracking
        public decimal EstimatedCost { get; set; }
        public decimal ActualCost { get; set; }

        public List<Comment> Comments { get; set; } = new();
        public List<Attachment> Attachments { get; set; } = new();
        public List<TimeEntry> TimeEntries { get; set; } = new();
        public List<CustomFieldValue> CustomFieldValues { get; set; } = new();
    }

    public sealed class EpicWorkItem : WorkItem
    {
        public EpicWorkItem()
        {
            Type = WorkItemType.Epic;
        }
    }

    public sealed class UserStoryWorkItem : WorkItem
    {
        public UserStoryWorkItem()
        {
            Type = WorkItemType.UserStory;
        }
    }

    public sealed class TaskWorkItem : WorkItem
    {
        public TaskWorkItem()
        {
            Type = WorkItemType.Task;
        }
    }

    public sealed class SubTaskWorkItem : WorkItem
    {
        public SubTaskWorkItem()
        {
            Type = WorkItemType.SubTask;
        }
    }

    public enum BugSeverity
    {
        Low = 1,
        Medium = 2,
        High = 3,
        Critical = 4
    }

    public sealed class BugWorkItem : WorkItem
    {
        public BugWorkItem()
        {
            Type = WorkItemType.Bug;
        }

        public BugSeverity Severity { get; set; } = BugSeverity.Medium;
        public string? StepsToReproduce { get; set; }
        public string? ExpectedBehavior { get; set; }
        public string? ActualBehavior { get; set; }
        public string? Environment { get; set; }
    }
}
