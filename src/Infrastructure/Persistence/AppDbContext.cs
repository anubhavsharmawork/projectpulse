using Application.Common.Interfaces;
using Domain.Attributes;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence
{
    public class AppDbContext : DbContext, IAppDbContext
    {
        private readonly ITenantService? _tenantService;
        private readonly IEncryptionService? _encryptionService;

        /// <summary>
        /// Current tenant ID for query filter evaluation. Returns Guid.Empty when no tenant
        /// context is available (e.g., during migrations or in unit tests without tenant service),
        /// which effectively disables filtering since no entity should have Guid.Empty as TenantId.
        /// </summary>
        private Guid _currentTenantId => _tenantService != null ? SafeGetTenantId() : Guid.Empty;

        private Guid SafeGetTenantId()
        {
            try { return _tenantService!.GetCurrentTenantId(); }
            catch { return Guid.Empty; }
        }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public AppDbContext(DbContextOptions<AppDbContext> options, ITenantService tenantService) : base(options)
        {
            _tenantService = tenantService;
        }

        public AppDbContext(
            DbContextOptions<AppDbContext> options,
            ITenantService tenantService,
            IEncryptionService encryptionService) : base(options)
        {
            _tenantService = tenantService;
            _encryptionService = encryptionService;
        }

        public DbSet<Tenant> Tenants => Set<Tenant>();
        public DbSet<User> Users => Set<User>();
        public DbSet<Project> Projects => Set<Project>();
        public DbSet<WorkItem> WorkItems => Set<WorkItem>();
        public DbSet<Comment> Comments => Set<Comment>();
        public DbSet<MentionNotification> MentionNotifications => Set<MentionNotification>();
        public DbSet<CustomField> CustomFields => Set<CustomField>();
        public DbSet<CustomFieldValue> CustomFieldValues => Set<CustomFieldValue>();
        public DbSet<Workflow> Workflows => Set<Workflow>();
        public DbSet<WorkflowState> WorkflowStates => Set<WorkflowState>();
        public DbSet<Team> Teams => Set<Team>();
        public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
        public DbSet<Attachment> Attachments => Set<Attachment>();
        public DbSet<TimeEntry> TimeEntries => Set<TimeEntry>();
        public DbSet<Relation> Relations => Set<Relation>();
        public DbSet<DomainTemplate> DomainTemplates => Set<DomainTemplate>();
        public DbSet<AppRole> AppRoles => Set<AppRole>();
        public DbSet<Permission> Permissions => Set<Permission>();
        public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
        public DbSet<WorkflowTransition> WorkflowTransitions => Set<WorkflowTransition>();
        public DbSet<ProjectCategory> ProjectCategories => Set<ProjectCategory>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<ProjectRole> ProjectRoles => Set<ProjectRole>();
        public DbSet<Feedback> Feedbacks => Set<Feedback>();

        // Asset Management
        public DbSet<Asset> Assets => Set<Asset>();
        public DbSet<MaintenanceRecord> MaintenanceRecords => Set<MaintenanceRecord>();
        public DbSet<AssetHistoryEntry> AssetHistoryEntries => Set<AssetHistoryEntry>();
        public DbSet<AssetCheckout> AssetCheckouts => Set<AssetCheckout>();
        public DbSet<DomainAssetConfig> DomainAssetConfigs => Set<DomainAssetConfig>();
        public DbSet<LegalDocument> LegalDocuments => Set<LegalDocument>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ── Project ──
            modelBuilder.Entity<Project>()
                .HasMany(p => p.WorkItems)
                .WithOne()
                .HasForeignKey(w => w.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Project>()
                .HasOne(p => p.Template)
                .WithMany()
                .HasForeignKey(p => p.TemplateId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Project>()
                .HasOne(p => p.Workflow)
                .WithMany()
                .HasForeignKey(p => p.WorkflowId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Project>()
                .HasMany(p => p.Teams)
                .WithOne(t => t.Project)
                .HasForeignKey(t => t.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Project>()
                .Property(p => p.DomainType)
                .HasConversion<string>();

            // ── WorkItem (TPH hierarchy) ──
            modelBuilder.Entity<WorkItem>()
                .HasMany(w => w.Children)
                .WithOne(w => w.Parent)
                .HasForeignKey(w => w.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WorkItem>()
                .HasMany(w => w.Comments)
                .WithOne()
                .HasForeignKey(c => c.WorkItemId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<WorkItem>()
                .HasDiscriminator(w => w.Type)
                .HasValue<EpicWorkItem>(WorkItemType.Epic)
                .HasValue<UserStoryWorkItem>(WorkItemType.UserStory)
                .HasValue<TaskWorkItem>(WorkItemType.Task)
                .HasValue<SubTaskWorkItem>(WorkItemType.SubTask)
                .HasValue<BugWorkItem>(WorkItemType.Bug);

            modelBuilder.Entity<WorkItem>()
                .HasIndex(w => w.ProjectId);

            modelBuilder.Entity<WorkItem>()
                .HasIndex(w => w.ParentId);

            modelBuilder.Entity<WorkItem>()
                .HasOne(w => w.CurrentState)
                .WithMany()
                .HasForeignKey(w => w.CurrentStateId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<WorkItem>()
                .HasMany(w => w.Attachments)
                .WithOne(a => a.WorkItem)
                .HasForeignKey(a => a.WorkItemId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<WorkItem>()
                .HasMany(w => w.TimeEntries)
                .WithOne(te => te.WorkItem)
                .HasForeignKey(te => te.WorkItemId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<WorkItem>()
                .HasMany(w => w.CustomFieldValues)
                .WithOne()
                .HasForeignKey(cfv => cfv.EntityId)
                .OnDelete(DeleteBehavior.Cascade);

            // ── User ──
            modelBuilder.Entity<User>()
                .Property(u => u.Email)
                .IsRequired();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.UserName)
                .IsUnique();

            modelBuilder.Entity<User>()
                .Property(u => u.UserName)
                .HasMaxLength(100);

            // ── Comment ──
            modelBuilder.Entity<Comment>()
                .Property(c => c.MentionedUserIds)
                .HasConversion(
                    v => v == null || v.Count == 0 ? "" : string.Join(',', v),
                    v => string.IsNullOrEmpty(v) 
                        ? new List<Guid>() 
                        : ParseGuidList(v)
                );

            // ── MentionNotification ──
            modelBuilder.Entity<MentionNotification>()
                .HasIndex(n => n.UserId);

            modelBuilder.Entity<MentionNotification>()
                .HasIndex(n => new { n.UserId, n.IsRead });

            // ── CustomField ──
            modelBuilder.Entity<CustomField>()
                .Property(cf => cf.FieldType)
                .HasConversion<string>();

            modelBuilder.Entity<CustomField>()
                .Property(cf => cf.DomainType)
                .HasConversion<string>();

            modelBuilder.Entity<CustomField>()
                .HasMany(cf => cf.Values)
                .WithOne(cfv => cfv.CustomField)
                .HasForeignKey(cfv => cfv.CustomFieldId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CustomField>()
                .HasOne(cf => cf.DomainTemplate)
                .WithMany(dt => dt.CustomFields)
                .HasForeignKey(cf => cf.DomainTemplateId)
                .OnDelete(DeleteBehavior.SetNull);

            // ── CustomFieldValue ──
            modelBuilder.Entity<CustomFieldValue>()
                .HasIndex(cfv => new { cfv.EntityId, cfv.CustomFieldId });

            // ── Workflow ──
            modelBuilder.Entity<Workflow>()
                .Property(w => w.DomainType)
                .HasConversion<string>();

            modelBuilder.Entity<Workflow>()
                .HasMany(w => w.States)
                .WithOne(ws => ws.Workflow)
                .HasForeignKey(ws => ws.WorkflowId)
                .OnDelete(DeleteBehavior.Cascade);

            // ── Team / TeamMember ──
            modelBuilder.Entity<TeamMember>()
                .HasOne(tm => tm.User)
                .WithMany()
                .HasForeignKey(tm => tm.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // ── Relation ──
            modelBuilder.Entity<Relation>()
                .HasOne(r => r.SourceWorkItem)
                .WithMany()
                .HasForeignKey(r => r.SourceWorkItemId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Relation>()
                .HasOne(r => r.TargetWorkItem)
                .WithMany()
                .HasForeignKey(r => r.TargetWorkItemId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Relation>()
                .Property(r => r.RelationType)
                .HasConversion<string>();

            modelBuilder.Entity<Relation>()
                .HasIndex(r => new { r.SourceWorkItemId, r.TargetWorkItemId });

            // ── TimeEntry ──
            modelBuilder.Entity<TimeEntry>()
                .HasOne(te => te.User)
                .WithMany()
                .HasForeignKey(te => te.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TimeEntry>()
                .Property(te => te.Hours)
                .HasPrecision(10, 2);

            // ── DomainTemplate ──
            modelBuilder.Entity<DomainTemplate>()
                .Property(dt => dt.DomainType)
                .HasConversion<string>();

            modelBuilder.Entity<DomainTemplate>()
                .HasOne(dt => dt.DefaultWorkflow)
                .WithMany()
                .HasForeignKey(dt => dt.DefaultWorkflowId)
                .OnDelete(DeleteBehavior.SetNull);

            // ── AppRole ──
            modelBuilder.Entity<AppRole>()
                .Property(r => r.SystemRole)
                .HasConversion<string>();

            modelBuilder.Entity<AppRole>()
                .HasIndex(r => r.SystemRole)
                .IsUnique();

            // ── Permission ──
            modelBuilder.Entity<Permission>()
                .Property(p => p.Category)
                .HasConversion<string>();

            modelBuilder.Entity<Permission>()
                .HasIndex(p => p.Name)
                .IsUnique();

            // ── RolePermission ──
            modelBuilder.Entity<RolePermission>()
                .HasOne(rp => rp.AppRole)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(rp => rp.AppRoleId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RolePermission>()
                .HasOne(rp => rp.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(rp => rp.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RolePermission>()
                .HasIndex(rp => new { rp.AppRoleId, rp.PermissionId })
                .IsUnique();

            // ── User → AppRole ──
            modelBuilder.Entity<User>()
                .HasOne(u => u.AppRole)
                .WithMany()
                .HasForeignKey(u => u.AppRoleId)
                .OnDelete(DeleteBehavior.SetNull);

            // ── TeamMember decimal precision ──
            modelBuilder.Entity<TeamMember>()
                .Property(tm => tm.AvailabilityHoursPerWeek)
                .HasPrecision(10, 2);

            modelBuilder.Entity<TeamMember>()
                .Property(tm => tm.CostRate)
                .HasPrecision(10, 2);

            // ── WorkflowTransition ──
            modelBuilder.Entity<WorkflowTransition>()
                .HasOne(wt => wt.WorkItem)
                .WithMany()
                .HasForeignKey(wt => wt.WorkItemId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<WorkflowTransition>()
                .HasOne(wt => wt.FromState)
                .WithMany()
                .HasForeignKey(wt => wt.FromStateId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WorkflowTransition>()
                .HasOne(wt => wt.ToState)
                .WithMany()
                .HasForeignKey(wt => wt.ToStateId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WorkflowTransition>()
                .HasOne(wt => wt.TransitionedByUser)
                .WithMany()
                .HasForeignKey(wt => wt.TransitionedByUserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<WorkflowTransition>()
                .HasIndex(wt => wt.WorkItemId);

            // ── ProjectCategory ──
            modelBuilder.Entity<ProjectCategory>()
                .Property(pc => pc.DomainType)
                .HasConversion<string>();

            // ── Project budget precision ──
            modelBuilder.Entity<Project>()
                .Property(p => p.EstimatedCost)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Project>()
                .Property(p => p.ActualCost)
                .HasPrecision(18, 2);

            // ── WorkItem budget precision ──
            modelBuilder.Entity<WorkItem>()
                .Property(w => w.EstimatedCost)
                .HasPrecision(18, 2);

            modelBuilder.Entity<WorkItem>()
                .Property(w => w.ActualCost)
                .HasPrecision(18, 2);

            // ── BugWorkItem properties ──
            modelBuilder.Entity<BugWorkItem>()
                .Property(b => b.Severity)
                .HasConversion<int>();

            // ── AuditLog ──
            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => a.EntityType);

            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => a.Timestamp);

            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => new { a.EntityType, a.EntityId });

            // ── Notification ──
            modelBuilder.Entity<Notification>()
                .Property(n => n.Type)
                .HasConversion<string>();

            modelBuilder.Entity<Notification>()
                .HasIndex(n => n.UserId);

            modelBuilder.Entity<Notification>()
                .HasIndex(n => new { n.UserId, n.IsRead });

            // ── ProjectRole ──
            modelBuilder.Entity<ProjectRole>()
                .HasOne(pr => pr.Project)
                .WithMany()
                .HasForeignKey(pr => pr.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProjectRole>()
                .Property(pr => pr.DomainType)
                .HasConversion<string>();

            modelBuilder.Entity<ProjectRole>()
                .HasIndex(pr => new { pr.ProjectId, pr.RoleName })
                .IsUnique();

            // ── Feedback ──
            modelBuilder.Entity<Feedback>()
                .Property(f => f.Message)
                .HasMaxLength(2000)
                .IsRequired();

            modelBuilder.Entity<Feedback>()
                .HasIndex(f => f.UserId);

            // ── Asset (flat table — no TPH hierarchy) ──
            modelBuilder.Entity<Asset>()
                .ToTable("Assets");

            modelBuilder.Entity<Asset>()
                .Property(a => a.Type)
                .HasConversion<int>();

            modelBuilder.Entity<Asset>()
                .Property(a => a.Status)
                .HasConversion<int>();

            modelBuilder.Entity<Asset>()
                .Property(a => a.DepreciationMethod)
                .HasConversion<int>();

            modelBuilder.Entity<Asset>()
                .Property(a => a.Category)
                .HasConversion<int>();

            modelBuilder.Entity<Asset>()
                .Property(a => a.Name)
                .HasMaxLength(200)
                .IsRequired();

            modelBuilder.Entity<Asset>()
                .Property(a => a.AssetTag)
                .HasMaxLength(50)
                .IsRequired();

            modelBuilder.Entity<Asset>()
                .Property(a => a.SerialNumber)
                .HasMaxLength(100);

            modelBuilder.Entity<Asset>()
                .Property(a => a.Manufacturer)
                .HasMaxLength(100);

            modelBuilder.Entity<Asset>()
                .Property(a => a.Model)
                .HasMaxLength(100);

            modelBuilder.Entity<Asset>()
                .Property(a => a.LicenseKey)
                .HasMaxLength(500);

            modelBuilder.Entity<Asset>()
                .Property(a => a.Vendor)
                .HasMaxLength(200);

            modelBuilder.Entity<Asset>()
                .Property(a => a.GridReference)
                .HasMaxLength(100);

            modelBuilder.Entity<Asset>()
                .Property(a => a.Capacity)
                .HasMaxLength(100);

            modelBuilder.Entity<Asset>()
                .Property(a => a.RegulatoryId)
                .HasMaxLength(100);

            modelBuilder.Entity<Asset>()
                .HasIndex(a => a.AssetTag)
                .IsUnique();

            modelBuilder.Entity<Asset>()
                .HasIndex(a => a.ProjectId);

            modelBuilder.Entity<Asset>()
                .HasIndex(a => a.Status);

            modelBuilder.Entity<Asset>()
                .HasIndex(a => a.AssignedToUserId);

            modelBuilder.Entity<Asset>()
                .HasIndex(a => a.PurchaseDate);

            modelBuilder.Entity<Asset>()
                .Property(a => a.PurchasePrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Asset>()
                .Property(a => a.CurrentValue)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Asset>()
                .HasOne(a => a.Project)
                .WithMany()
                .HasForeignKey(a => a.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Asset>()
                .HasOne(a => a.AssignedToUser)
                .WithMany()
                .HasForeignKey(a => a.AssignedToUserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Asset>()
                .HasMany(a => a.MaintenanceRecords)
                .WithOne(m => m.Asset)
                .HasForeignKey(m => m.AssetId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Asset>()
                .HasMany(a => a.AssetHistory)
                .WithOne(h => h.Asset)
                .HasForeignKey(h => h.AssetId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Asset>()
                .HasMany(a => a.AssetCheckouts)
                .WithOne(c => c.Asset)
                .HasForeignKey(c => c.AssetId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Asset>()
                .HasOne(a => a.DomainAssetConfig)
                .WithMany()
                .HasForeignKey(a => a.DomainAssetConfigId)
                .OnDelete(DeleteBehavior.SetNull);

            // ── PhysicalAsset properties (now on Asset) ──
            modelBuilder.Entity<Asset>()
                .Property(p => p.Weight)
                .HasPrecision(10, 2);

            // ── MaintenanceRecord ──
            modelBuilder.Entity<MaintenanceRecord>()
                .Property(m => m.MaintenanceType)
                .HasConversion<int>();

            modelBuilder.Entity<MaintenanceRecord>()
                .Property(m => m.Cost)
                .HasPrecision(18, 2);

            modelBuilder.Entity<MaintenanceRecord>()
                .HasIndex(m => m.AssetId);

            modelBuilder.Entity<MaintenanceRecord>()
                .HasIndex(m => m.ScheduledDate);

            // ── DomainAssetConfig ──
            modelBuilder.Entity<DomainAssetConfig>()
                .Property(c => c.DomainType)
                .HasConversion<string>();

            modelBuilder.Entity<DomainAssetConfig>()
                .Property(c => c.AssetType)
                .HasConversion<int>();

            modelBuilder.Entity<DomainAssetConfig>()
                .Property(c => c.Category)
                .HasConversion<int>();

            modelBuilder.Entity<DomainAssetConfig>()
                .Property(c => c.DefaultDepreciationMethod)
                .HasConversion<int>();

            modelBuilder.Entity<DomainAssetConfig>()
                .Property(c => c.DisplayLabel)
                .HasMaxLength(200)
                .IsRequired();

            modelBuilder.Entity<DomainAssetConfig>()
                .Property(c => c.ComplianceNotes)
                .HasMaxLength(1000);

            modelBuilder.Entity<DomainAssetConfig>()
                .HasIndex(c => new { c.DomainType, c.AssetType })
                .IsUnique();

            // ── AssetHistoryEntry ──
            modelBuilder.Entity<AssetHistoryEntry>()
                .Property(h => h.ChangeType)
                .HasConversion<int>();

            modelBuilder.Entity<AssetHistoryEntry>()
                .HasIndex(h => new { h.AssetId, h.ChangedAt });

            // ── AssetCheckout ──
            modelBuilder.Entity<AssetCheckout>()
                .HasOne(c => c.CheckedOutToUser)
                .WithMany()
                .HasForeignKey(c => c.CheckedOutToUserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AssetCheckout>()
                .HasIndex(c => c.AssetId);

            modelBuilder.Entity<AssetCheckout>()
                .HasIndex(c => c.CheckedOutToUserId);

            // ── Tenant ──
            modelBuilder.Entity<Tenant>()
                .HasIndex(t => t.Subdomain)
                .IsUnique();

            modelBuilder.Entity<Tenant>()
                .Property(t => t.Name)
                .HasMaxLength(200)
                .IsRequired();

            modelBuilder.Entity<Tenant>()
                .Property(t => t.Subdomain)
                .HasMaxLength(50)
                .IsRequired();

            modelBuilder.Entity<Tenant>()
                .Property(t => t.Tier)
                .HasConversion<string>();

            // ── LegalDocument ──
            modelBuilder.Entity<LegalDocument>()
                .Property(d => d.DocumentType)
                .HasConversion<string>();

            modelBuilder.Entity<LegalDocument>()
                .Property(d => d.Version)
                .HasMaxLength(20)
                .IsRequired();

            modelBuilder.Entity<LegalDocument>()
                .HasIndex(d => new { d.DocumentType, d.IsActive });

            // ── User timezone & legal fields ──
            modelBuilder.Entity<User>()
                .Property(u => u.TimeZoneId)
                .HasMaxLength(100);

            modelBuilder.Entity<User>()
                .Property(u => u.TermsVersion)
                .HasMaxLength(20);

            modelBuilder.Entity<User>()
                .Property(u => u.PrivacyVersion)
                .HasMaxLength(20);

            modelBuilder.Entity<User>()
                .Property(u => u.LegalAcceptanceIp)
                .HasMaxLength(45);

            // ── TenantId indexes for query performance ──
            modelBuilder.Entity<User>().HasIndex(e => e.TenantId);
            modelBuilder.Entity<Project>().HasIndex(e => e.TenantId);
            modelBuilder.Entity<WorkItem>().HasIndex(e => e.TenantId);
            modelBuilder.Entity<Comment>().HasIndex(e => e.TenantId);
            modelBuilder.Entity<Team>().HasIndex(e => e.TenantId);
            modelBuilder.Entity<TeamMember>().HasIndex(e => e.TenantId);
            modelBuilder.Entity<TimeEntry>().HasIndex(e => e.TenantId);
            modelBuilder.Entity<Notification>().HasIndex(e => e.TenantId);
            modelBuilder.Entity<MentionNotification>().HasIndex(e => e.TenantId);
            modelBuilder.Entity<CustomField>().HasIndex(e => e.TenantId);
            modelBuilder.Entity<CustomFieldValue>().HasIndex(e => e.TenantId);
            modelBuilder.Entity<Feedback>().HasIndex(e => e.TenantId);
            modelBuilder.Entity<Workflow>().HasIndex(e => e.TenantId);
            modelBuilder.Entity<WorkflowState>().HasIndex(e => e.TenantId);
            modelBuilder.Entity<WorkflowTransition>().HasIndex(e => e.TenantId);
            modelBuilder.Entity<AuditLog>().HasIndex(e => e.TenantId);
            modelBuilder.Entity<Attachment>().HasIndex(e => e.TenantId);
            modelBuilder.Entity<Relation>().HasIndex(e => e.TenantId);
            modelBuilder.Entity<ProjectRole>().HasIndex(e => e.TenantId);
            modelBuilder.Entity<ProjectCategory>().HasIndex(e => e.TenantId);
            modelBuilder.Entity<Asset>().HasIndex(e => e.TenantId);
            modelBuilder.Entity<AssetCheckout>().HasIndex(e => e.TenantId);
            modelBuilder.Entity<AssetHistoryEntry>().HasIndex(e => e.TenantId);
            modelBuilder.Entity<MaintenanceRecord>().HasIndex(e => e.TenantId);

            // ── Global query filters for tenant isolation ──
            // When _currentTenantId is Guid.Empty (no tenant service or no context), filter is
            // effectively disabled because no production entity should have TenantId == Guid.Empty.
            // When running with a real tenant context, only that tenant's data is returned.
            // SystemAdmin bypass is handled at the query level with IgnoreQueryFilters().
            modelBuilder.Entity<User>().HasQueryFilter(e => _currentTenantId == Guid.Empty || e.TenantId == _currentTenantId);
            modelBuilder.Entity<Project>().HasQueryFilter(e => _currentTenantId == Guid.Empty || e.TenantId == _currentTenantId);
            modelBuilder.Entity<WorkItem>().HasQueryFilter(e => _currentTenantId == Guid.Empty || e.TenantId == _currentTenantId);
            modelBuilder.Entity<Comment>().HasQueryFilter(e => _currentTenantId == Guid.Empty || e.TenantId == _currentTenantId);
            modelBuilder.Entity<Team>().HasQueryFilter(e => _currentTenantId == Guid.Empty || e.TenantId == _currentTenantId);
            modelBuilder.Entity<TeamMember>().HasQueryFilter(e => _currentTenantId == Guid.Empty || e.TenantId == _currentTenantId);
            modelBuilder.Entity<TimeEntry>().HasQueryFilter(e => _currentTenantId == Guid.Empty || e.TenantId == _currentTenantId);
            modelBuilder.Entity<Notification>().HasQueryFilter(e => _currentTenantId == Guid.Empty || e.TenantId == _currentTenantId);
            modelBuilder.Entity<MentionNotification>().HasQueryFilter(e => _currentTenantId == Guid.Empty || e.TenantId == _currentTenantId);
            modelBuilder.Entity<CustomField>().HasQueryFilter(e => _currentTenantId == Guid.Empty || e.TenantId == _currentTenantId);
            modelBuilder.Entity<CustomFieldValue>().HasQueryFilter(e => _currentTenantId == Guid.Empty || e.TenantId == _currentTenantId);
            modelBuilder.Entity<Feedback>().HasQueryFilter(e => _currentTenantId == Guid.Empty || e.TenantId == _currentTenantId);
            modelBuilder.Entity<Workflow>().HasQueryFilter(e => _currentTenantId == Guid.Empty || e.TenantId == _currentTenantId);
            modelBuilder.Entity<WorkflowState>().HasQueryFilter(e => _currentTenantId == Guid.Empty || e.TenantId == _currentTenantId);
            modelBuilder.Entity<WorkflowTransition>().HasQueryFilter(e => _currentTenantId == Guid.Empty || e.TenantId == _currentTenantId);
            modelBuilder.Entity<AuditLog>().HasQueryFilter(e => _currentTenantId == Guid.Empty || e.TenantId == _currentTenantId);
            modelBuilder.Entity<Attachment>().HasQueryFilter(e => _currentTenantId == Guid.Empty || e.TenantId == _currentTenantId);
            modelBuilder.Entity<Relation>().HasQueryFilter(e => _currentTenantId == Guid.Empty || e.TenantId == _currentTenantId);
            modelBuilder.Entity<ProjectRole>().HasQueryFilter(e => _currentTenantId == Guid.Empty || e.TenantId == _currentTenantId);
            modelBuilder.Entity<ProjectCategory>().HasQueryFilter(e => _currentTenantId == Guid.Empty || e.TenantId == _currentTenantId);
            modelBuilder.Entity<Asset>().HasQueryFilter(e => _currentTenantId == Guid.Empty || e.TenantId == _currentTenantId);
            modelBuilder.Entity<AssetCheckout>().HasQueryFilter(e => _currentTenantId == Guid.Empty || e.TenantId == _currentTenantId);
            modelBuilder.Entity<AssetHistoryEntry>().HasQueryFilter(e => _currentTenantId == Guid.Empty || e.TenantId == _currentTenantId);
            modelBuilder.Entity<MaintenanceRecord>().HasQueryFilter(e => _currentTenantId == Guid.Empty || e.TenantId == _currentTenantId);

            // ── Field-level encryption via [Encrypted] attribute ──
            // Discovers all string properties marked with [Encrypted] and applies
            // an EF Core value converter that transparently encrypts on write / decrypts on read.
            if (_encryptionService is not null)
            {
                var converter = new EncryptedStringConverter(_encryptionService, () => _currentTenantId);

                foreach (var entityType in modelBuilder.Model.GetEntityTypes())
                {
                    foreach (var property in entityType.GetProperties())
                    {
                        if (property.ClrType != typeof(string))
                            continue;

                        var clrProperty = property.PropertyInfo;
                        if (clrProperty is null)
                            continue;

                        var hasEncrypted = clrProperty.GetCustomAttributes(typeof(EncryptedAttribute), true).Length > 0;
                        if (hasEncrypted)
                        {
                            property.SetValueConverter(converter);
                            // Encrypted values are longer than plaintext; ensure column can hold them
                            if ((property.GetMaxLength() ?? 0) < 1024)
                                property.SetMaxLength(1024);
                        }
                    }
                }
            }
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            if (_tenantService is not null)
            {
                Guid? tenantId = null;
                try { tenantId = _tenantService.GetCurrentTenantId(); } catch { /* no tenant context available */ }

                if (tenantId.HasValue && tenantId.Value != Guid.Empty)
                {
                    foreach (var entry in ChangeTracker.Entries().Where(e => e.State == EntityState.Added))
                    {
                        var tenantIdProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "TenantId");
                        if (tenantIdProp is not null && tenantIdProp.CurrentValue is Guid currentVal && currentVal == Guid.Empty)
                        {
                            tenantIdProp.CurrentValue = tenantId.Value;
                        }
                    }
                }
            }

            return await base.SaveChangesAsync(cancellationToken);
        }

        private static List<Guid> ParseGuidList(string value)
        {
            var result = new List<Guid>();
            if (string.IsNullOrEmpty(value)) return result;

            foreach (var part in value.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                if (Guid.TryParse(part, out var guid))
                {
                    result.Add(guid);
                }
            }
            return result;
        }
    }
}
