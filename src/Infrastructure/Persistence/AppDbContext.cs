using Application.Common.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence
{
    public class AppDbContext : DbContext, IAppDbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Project> Projects => Set<Project>();
        public DbSet<WorkItem> WorkItems => Set<WorkItem>();
        public DbSet<Comment> Comments => Set<Comment>();
        public DbSet<MentionNotification> MentionNotifications => Set<MentionNotification>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Project>()
                .HasMany(p => p.WorkItems)
                .WithOne()
                .HasForeignKey(w => w.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

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
                .HasValue<TaskWorkItem>(WorkItemType.Task);

            modelBuilder.Entity<WorkItem>()
                .HasIndex(w => w.ProjectId);

            modelBuilder.Entity<WorkItem>()
                .HasIndex(w => w.ParentId);

            modelBuilder.Entity<User>()
                .Property(u => u.Email)
                .IsRequired();

            // Comment configuration - store MentionedUserIds as comma-separated string
            modelBuilder.Entity<Comment>()
                .Property(c => c.MentionedUserIds)
                .HasConversion(
                    v => v == null || v.Count == 0 ? "" : string.Join(',', v),
                    v => string.IsNullOrEmpty(v) 
                        ? new List<Guid>() 
                        : ParseGuidList(v)
                );

            // MentionNotification configuration
            modelBuilder.Entity<MentionNotification>()
                .HasIndex(n => n.UserId);

            modelBuilder.Entity<MentionNotification>()
                .HasIndex(n => new { n.UserId, n.IsRead });
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
