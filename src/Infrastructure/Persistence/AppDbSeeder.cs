using Infrastructure.Sql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

namespace Infrastructure.Persistence
{
    [ExcludeFromCodeCoverage]
    public static class AppDbSeeder
    {
        public static async Task SeedDemoAsync(this IServiceProvider services, ILogger logger)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Ensure base schema exists defensively in case a deploy missed migrations
            try
            {
                await db.Database.ExecuteSqlRawAsync(SqlLoader.EnsureBaseSchema);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Defensive base schema creation during seed encountered an error; proceeding.");
            }

            if (!await db.Users.AnyAsync())
            {
                var salt = Environment.GetEnvironmentVariable("DEMO_SALT") ?? "demo-salt";
                var demoAdmin = new Domain.Entities.User
                {
                    Id = Guid.NewGuid(),
                    Email = "admin@demo.local",
                    DisplayName = "Demo Admin",
                    PasswordHash = Application.Common.Security.SimplePasswordHasher.Hash("demo123!", salt),
                    Role = Domain.Entities.Role.Admin
                };
                db.Users.Add(demoAdmin);
                await db.SaveChangesAsync();
                logger.LogInformation("Seeded demo admin user: {Email} / demo123!", demoAdmin.Email);
            }
        }
    }
}
