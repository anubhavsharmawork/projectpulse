using Domain.Entities;

namespace Application.Common.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IRepository<CustomField> CustomFields { get; }
        IRepository<CustomFieldValue> CustomFieldValues { get; }
        IRepository<Workflow> Workflows { get; }
        IRepository<WorkflowState> WorkflowStates { get; }
        IRepository<Team> Teams { get; }
        IRepository<TeamMember> TeamMembers { get; }
        IRepository<Attachment> Attachments { get; }
        IRepository<TimeEntry> TimeEntries { get; }
        IRepository<Relation> Relations { get; }
        IRepository<DomainTemplate> DomainTemplates { get; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
