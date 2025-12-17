using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Interfaces
{
    public interface IAppDbContext
    {
        DbSet<User> Users { get; }
        DbSet<Project> Projects { get; }
        DbSet<WorkItem> WorkItems { get; }
        DbSet<Comment> Comments { get; }
        DbSet<MentionNotification> MentionNotifications { get; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
