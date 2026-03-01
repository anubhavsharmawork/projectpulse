using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Interfaces
{
    public interface IAppDbContext
    {
        DbSet<Tenant> Tenants { get; }
        DbSet<User> Users { get; }
        DbSet<Project> Projects { get; }
        DbSet<WorkItem> WorkItems { get; }
        DbSet<Comment> Comments { get; }
        DbSet<MentionNotification> MentionNotifications { get; }
        DbSet<CustomField> CustomFields { get; }
        DbSet<CustomFieldValue> CustomFieldValues { get; }
        DbSet<Workflow> Workflows { get; }
        DbSet<WorkflowState> WorkflowStates { get; }
        DbSet<Team> Teams { get; }
        DbSet<TeamMember> TeamMembers { get; }
        DbSet<Attachment> Attachments { get; }
        DbSet<TimeEntry> TimeEntries { get; }
        DbSet<Relation> Relations { get; }
        DbSet<DomainTemplate> DomainTemplates { get; }
        DbSet<AppRole> AppRoles { get; }
        DbSet<Permission> Permissions { get; }
        DbSet<RolePermission> RolePermissions { get; }
        DbSet<WorkflowTransition> WorkflowTransitions { get; }
        DbSet<ProjectCategory> ProjectCategories { get; }
        DbSet<AuditLog> AuditLogs { get; }
        DbSet<Notification> Notifications { get; }
        DbSet<ProjectRole> ProjectRoles { get; }
        DbSet<Feedback> Feedbacks { get; }

        // Asset Management
        DbSet<Asset> Assets { get; }
        DbSet<MaintenanceRecord> MaintenanceRecords { get; }
        DbSet<AssetHistoryEntry> AssetHistoryEntries { get; }
        DbSet<AssetCheckout> AssetCheckouts { get; }
        DbSet<DomainAssetConfig> DomainAssetConfigs { get; }
        DbSet<LegalDocument> LegalDocuments { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
