using Application.Common.Interfaces;
using Domain.Entities;

namespace Infrastructure.Persistence
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        private IRepository<CustomField>? _customFields;
        private IRepository<CustomFieldValue>? _customFieldValues;
        private IRepository<Workflow>? _workflows;
        private IRepository<WorkflowState>? _workflowStates;
        private IRepository<Team>? _teams;
        private IRepository<TeamMember>? _teamMembers;
        private IRepository<Attachment>? _attachments;
        private IRepository<TimeEntry>? _timeEntries;
        private IRepository<Relation>? _relations;
        private IRepository<DomainTemplate>? _domainTemplates;

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
        }

        public IRepository<CustomField> CustomFields =>
            _customFields ??= new Repository<CustomField>(_context);

        public IRepository<CustomFieldValue> CustomFieldValues =>
            _customFieldValues ??= new Repository<CustomFieldValue>(_context);

        public IRepository<Workflow> Workflows =>
            _workflows ??= new Repository<Workflow>(_context);

        public IRepository<WorkflowState> WorkflowStates =>
            _workflowStates ??= new Repository<WorkflowState>(_context);

        public IRepository<Team> Teams =>
            _teams ??= new Repository<Team>(_context);

        public IRepository<TeamMember> TeamMembers =>
            _teamMembers ??= new Repository<TeamMember>(_context);

        public IRepository<Attachment> Attachments =>
            _attachments ??= new Repository<Attachment>(_context);

        public IRepository<TimeEntry> TimeEntries =>
            _timeEntries ??= new Repository<TimeEntry>(_context);

        public IRepository<Relation> Relations =>
            _relations ??= new Repository<Relation>(_context);

        public IRepository<DomainTemplate> DomainTemplates =>
            _domainTemplates ??= new Repository<DomainTemplate>(_context);

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
