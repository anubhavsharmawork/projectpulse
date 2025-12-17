namespace Domain.Entities
{
    public enum WorkItemType
    {
        Epic = 1,
        UserStory = 2,
        Task = 3
    }

    public abstract class WorkItem
    {
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }
        public Guid? ParentId { get; set; }
        public WorkItem? Parent { get; set; }
        public List<WorkItem> Children { get; set; } = new();
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? AttachmentUrl { get; set; }
        public bool IsCompleted { get; set; }
        public Guid? AssigneeId { get; set; }
        public List<Comment> Comments { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public WorkItemType Type { get; protected set; }
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
}
