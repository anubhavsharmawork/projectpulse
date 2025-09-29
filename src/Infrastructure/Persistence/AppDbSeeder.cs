using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence
{
    public static class AppDbSeeder
    {
        public static async Task SeedDemoAsync(this IServiceProvider services, ILogger logger)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Ensure base schema exists defensively in case a deploy missed migrations
            try
            {
                await db.Database.ExecuteSqlRawAsync(@"
                    CREATE TABLE IF NOT EXISTS ""Users"" (
                        ""Id"" uuid NOT NULL,
                        ""Email"" text NOT NULL,
                        ""PasswordHash"" text NOT NULL,
                        ""DisplayName"" text NOT NULL,
                        ""Role"" integer NOT NULL,
                        ""CreatedAt"" timestamp with time zone NOT NULL,
                        CONSTRAINT ""PK_Users"" PRIMARY KEY (""Id"")
                    );
                    CREATE TABLE IF NOT EXISTS ""Projects"" (
                        ""Id"" uuid NOT NULL,
                        ""Name"" text NOT NULL,
                        ""Description"" text NULL,
                        ""OwnerId"" uuid NOT NULL,
                        ""CreatedAt"" timestamp with time zone NOT NULL,
                        CONSTRAINT ""PK_Projects"" PRIMARY KEY (""Id"")
                    );
                    CREATE TABLE IF NOT EXISTS ""Tasks"" (
                        ""Id"" uuid NOT NULL,
                        ""ProjectId"" uuid NOT NULL,
                        ""Title"" text NOT NULL,
                        ""Description"" text NULL,
                        ""IsCompleted"" boolean NOT NULL,
                        ""AssigneeId"" uuid NULL,
                        ""CreatedAt"" timestamp with time zone NOT NULL,
                        ""CompletedAt"" timestamp with time zone NULL,
                        CONSTRAINT ""PK_Tasks"" PRIMARY KEY (""Id""),
                        CONSTRAINT ""FK_Tasks_Projects_ProjectId"" FOREIGN KEY (""ProjectId"") REFERENCES ""Projects"" (""Id"") ON DELETE CASCADE
                    );
                    CREATE TABLE IF NOT EXISTS ""Comments"" (
                        ""Id"" uuid NOT NULL,
                        ""TaskItemId"" uuid NOT NULL,
                        ""AuthorId"" uuid NOT NULL,
                        ""Body"" text NOT NULL,
                        ""CreatedAt"" timestamp with time zone NOT NULL,
                        CONSTRAINT ""PK_Comments"" PRIMARY KEY (""Id""),
                        CONSTRAINT ""FK_Comments_Tasks_TaskItemId"" FOREIGN KEY (""TaskItemId"") REFERENCES ""Tasks"" (""Id"") ON DELETE CASCADE
                    );
                    CREATE INDEX IF NOT EXISTS ""IX_Tasks_ProjectId"" ON ""Tasks"" (""ProjectId"");
                    CREATE INDEX IF NOT EXISTS ""IX_Comments_TaskItemId"" ON ""Comments"" (""TaskItemId"");
                ");
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
