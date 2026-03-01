using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260207000000_AddUserNameAndProjectRoles")]
    public partial class AddUserNameAndProjectRoles : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add UserName column to Users table
            migrationBuilder.Sql(@"
                ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""UserName"" character varying(100) NOT NULL DEFAULT '';
            ");

            // Backfill UserName from email (part before @) for existing users
            migrationBuilder.Sql(@"
                UPDATE ""Users"" SET ""UserName"" = SPLIT_PART(""Email"", '@', 1) WHERE ""UserName"" = '' OR ""UserName"" IS NULL;
            ");

            // Add unique index on UserName
            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Users_UserName"" ON ""Users"" (""UserName"");
            ");

            // Create ProjectRoles table
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS ""ProjectRoles"" (
                    ""Id"" uuid NOT NULL,
                    ""ProjectId"" uuid NOT NULL,
                    ""RoleName"" text NOT NULL,
                    ""DomainType"" text,
                    ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    CONSTRAINT ""PK_ProjectRoles"" PRIMARY KEY (""Id""),
                    CONSTRAINT ""FK_ProjectRoles_Projects_ProjectId"" FOREIGN KEY (""ProjectId"") REFERENCES ""Projects"" (""Id"") ON DELETE CASCADE
                );
            ");

            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_ProjectRoles_ProjectId_RoleName"" ON ""ProjectRoles"" (""ProjectId"", ""RoleName"");
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS ""ProjectRoles"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Users_UserName"";");
            migrationBuilder.Sql(@"ALTER TABLE ""Users"" DROP COLUMN IF EXISTS ""UserName"";");
        }
    }
}
