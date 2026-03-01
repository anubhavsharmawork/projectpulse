using Domain.Constants;
using Infrastructure.Sql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

namespace Infrastructure.Persistence
{
    /// <summary>
    /// Seeds initial users for local/demo environments.
    /// The real admin account comes entirely from environment variables — no admin
    /// credentials are hardcoded in source, docs, or template files.
    /// The demo user is a non-admin account for quick testing.
    /// </summary>
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

            if (await db.Users.AnyAsync())
                return;

            // ── Admin user from environment variables ──
            // Only created when ADMIN_USERNAME and ADMIN_PASSWORD_HASH are set.
            // Use the PasswordHashGenerator tool to produce the hash offline.
            var adminEmail = Environment.GetEnvironmentVariable("ADMIN_USERNAME");
            var adminHash = Environment.GetEnvironmentVariable("ADMIN_PASSWORD_HASH");

            if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminHash))
            {
                var admin = new Domain.Entities.User
                {
                    Id = Guid.NewGuid(),
                    Email = adminEmail.Trim(),
                    DisplayName = "System Admin",
                    UserName = adminEmail.Trim().Split('@')[0],
                    PasswordHash = adminHash.Trim(),
                    Role = Domain.Entities.Role.Admin,
                    TenantId = TenantConstants.DefaultTenantId
                };
                db.Users.Add(admin);
                logger.LogInformation("Seeded admin user with role {Role} (email from ADMIN_USERNAME env var).", admin.Role);
            }
            else
            {
                logger.LogWarning(
                    "ADMIN_USERNAME or ADMIN_PASSWORD_HASH not set — skipping admin user creation. " +
                    "Set these environment variables and restart to create the admin account.");
            }

            // ── Demo user (member role with read-only SystemAdmin view via is_demo JWT claim) ──
            var demoSalt = Environment.GetEnvironmentVariable("DEMO_SALT") ?? "demo-salt";
            var demoUser = new Domain.Entities.User
            {
                Id = Guid.NewGuid(),
                Email = "demo@demo.local",
                DisplayName = "Demo User",
                UserName = "demo",
                PasswordHash = Application.Common.Security.SimplePasswordHasher.Hash("demo123!", demoSalt),
                Role = Domain.Entities.Role.Member,
                TenantId = TenantConstants.DefaultTenantId
            };
            db.Users.Add(demoUser);
            logger.LogInformation("Seeded demo user {Email} with role {Role}.", demoUser.Email, demoUser.Role);

            await db.SaveChangesAsync();
        }

        /// <summary>
        /// Links seeded users to their matching AppRoles (by SystemRole enum).
        /// Must run after both SeedDemoAsync and SeedRolesAndPermissionsAsync.
        /// </summary>
        public static async Task LinkUsersToAppRolesAsync(this IServiceProvider services, ILogger logger)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Only link users that have no AppRole assigned yet
            var unlinked = await db.Users
                .Where(u => u.AppRoleId == null)
                .ToListAsync();

            if (unlinked.Count == 0) return;

            var roles = await db.AppRoles.AsNoTracking().ToListAsync();
            if (roles.Count == 0) return;

            foreach (var user in unlinked)
            {
                Domain.Enums.SystemRole targetSystemRole;

                if (user.Email.Equals("demo@demo.local", StringComparison.OrdinalIgnoreCase))
                {
                    // Demo user gets SystemAdmin AppRole for read-only dashboard access.
                    // Write operations are blocked by SystemAdminWritePolicy (is_demo claim).
                    targetSystemRole = Domain.Enums.SystemRole.SystemAdmin;
                }
                else if (user.Role == Domain.Entities.Role.Admin)
                {
                    // Real admin users → SystemAdmin AppRole with full write access
                    targetSystemRole = Domain.Enums.SystemRole.SystemAdmin;
                }
                else
                {
                    // Member users → Viewer AppRole (least privilege)
                    targetSystemRole = Domain.Enums.SystemRole.Viewer;
                }

                var appRole = roles.FirstOrDefault(r => r.SystemRole == targetSystemRole);
                if (appRole != null)
                {
                    user.AppRoleId = appRole.Id;
                    logger.LogInformation("Linked user {Email} to AppRole {RoleName}.", user.Email, appRole.Name);
                }
            }

            await db.SaveChangesAsync();
        }
    }
}
