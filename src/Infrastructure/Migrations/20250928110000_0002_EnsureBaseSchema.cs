using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Infrastructure.Migrations
{
    // Safety migration to ensure the base schema exists (Users, Projects, Tasks, Comments)
    // in environments where the initial migration may have partially applied or legacy
    // databases are present.
    [DbContext(typeof(AppDbContext))]
    [Migration("20250928110000_0002_EnsureBaseSchema")] // unique id for EF
    public partial class _0002_EnsureBaseSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Postgres-specific DDL with IF NOT EXISTS to avoid errors when objects already exist
            migrationBuilder.Sql(@"
                -- Users
                CREATE TABLE IF NOT EXISTS ""Users"" (
                    ""Id"" uuid NOT NULL,
                    ""Email"" text NOT NULL,
                    ""PasswordHash"" text NOT NULL,
                    ""DisplayName"" text NOT NULL,
                    ""Role"" integer NOT NULL,
                    ""CreatedAt"" timestamp with time zone NOT NULL,
                    CONSTRAINT ""PK_Users"" PRIMARY KEY (""Id"")
                );

                -- Projects
                CREATE TABLE IF NOT EXISTS ""Projects"" (
                    ""Id"" uuid NOT NULL,
                    ""Name"" text NOT NULL,
                    ""Description"" text NULL,
                    ""OwnerId"" uuid NOT NULL,
                    ""CreatedAt"" timestamp with time zone NOT NULL,
                    CONSTRAINT ""PK_Projects"" PRIMARY KEY (""Id"")
                );

                -- Tasks (depends on Projects)
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

                -- Comments (depends on Tasks)
                CREATE TABLE IF NOT EXISTS ""Comments"" (
                    ""Id"" uuid NOT NULL,
                    ""TaskItemId"" uuid NOT NULL,
                    ""AuthorId"" uuid NOT NULL,
                    ""Body"" text NOT NULL,
                    ""CreatedAt"" timestamp with time zone NOT NULL,
                    CONSTRAINT ""PK_Comments"" PRIMARY KEY (""Id""),
                    CONSTRAINT ""FK_Comments_Tasks_TaskItemId"" FOREIGN KEY (""TaskItemId"") REFERENCES ""Tasks"" (""Id"") ON DELETE CASCADE
                );

                -- Indexes
                CREATE INDEX IF NOT EXISTS ""IX_Tasks_ProjectId"" ON ""Tasks"" (""ProjectId"");
                CREATE INDEX IF NOT EXISTS ""IX_Comments_TaskItemId"" ON ""Comments"" (""TaskItemId"");
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop in dependency order
            migrationBuilder.Sql(@"
                DROP TABLE IF EXISTS ""Comments"";
                DROP TABLE IF EXISTS ""Tasks"";
                DROP TABLE IF EXISTS ""Projects"";
                DROP TABLE IF EXISTS ""Users"";
            ");
        }
    }
}
