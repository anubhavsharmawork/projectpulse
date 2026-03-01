using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260202000000_RbacTeamManagement")]
    public partial class RbacTeamManagement : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── AppRoles ──
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS ""AppRoles"" (
                    ""Id""          uuid NOT NULL PRIMARY KEY,
                    ""Name""        text NOT NULL DEFAULT '',
                    ""SystemRole""  text NOT NULL DEFAULT '',
                    ""Description"" text,
                    ""CreatedAt""   timestamp without time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    ""UpdatedAt""   timestamp without time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    ""CreatedBy""   text NOT NULL DEFAULT '',
                    ""IsActive""    boolean NOT NULL DEFAULT true
                );
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_AppRoles_SystemRole""
                    ON ""AppRoles"" (""SystemRole"");
            ");

            // ── Permissions ──
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS ""Permissions"" (
                    ""Id""          uuid NOT NULL PRIMARY KEY,
                    ""Name""        text NOT NULL DEFAULT '',
                    ""Category""    text NOT NULL DEFAULT '',
                    ""Description"" text,
                    ""CreatedAt""   timestamp without time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    ""UpdatedAt""   timestamp without time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    ""CreatedBy""   text NOT NULL DEFAULT '',
                    ""IsActive""    boolean NOT NULL DEFAULT true
                );
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Permissions_Name""
                    ON ""Permissions"" (""Name"");
            ");

            // ── RolePermissions ──
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS ""RolePermissions"" (
                    ""Id""            uuid NOT NULL PRIMARY KEY,
                    ""AppRoleId""     uuid NOT NULL REFERENCES ""AppRoles""(""Id"") ON DELETE CASCADE,
                    ""PermissionId""  uuid NOT NULL REFERENCES ""Permissions""(""Id"") ON DELETE CASCADE,
                    ""CreatedAt""     timestamp without time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    ""UpdatedAt""     timestamp without time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    ""CreatedBy""     text NOT NULL DEFAULT '',
                    ""IsActive""      boolean NOT NULL DEFAULT true
                );
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_RolePermissions_AppRoleId_PermissionId""
                    ON ""RolePermissions"" (""AppRoleId"", ""PermissionId"");
            ");

            // ── Users — add AppRoleId FK ──
            migrationBuilder.Sql(@"
                ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""AppRoleId"" uuid REFERENCES ""AppRoles""(""Id"") ON DELETE SET NULL;
            ");

            // ── TeamMembers — add new columns ──
            migrationBuilder.Sql(@"
                ALTER TABLE ""TeamMembers"" ADD COLUMN IF NOT EXISTS ""DomainExpertise""          text;
                ALTER TABLE ""TeamMembers"" ADD COLUMN IF NOT EXISTS ""Skills""                   text;
                ALTER TABLE ""TeamMembers"" ADD COLUMN IF NOT EXISTS ""AvailabilityHoursPerWeek"" numeric(10,2) NOT NULL DEFAULT 40;
                ALTER TABLE ""TeamMembers"" ADD COLUMN IF NOT EXISTS ""CostRate""                 numeric(10,2) NOT NULL DEFAULT 0;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE ""TeamMembers"" DROP COLUMN IF EXISTS ""CostRate"";");
            migrationBuilder.Sql(@"ALTER TABLE ""TeamMembers"" DROP COLUMN IF EXISTS ""AvailabilityHoursPerWeek"";");
            migrationBuilder.Sql(@"ALTER TABLE ""TeamMembers"" DROP COLUMN IF EXISTS ""Skills"";");
            migrationBuilder.Sql(@"ALTER TABLE ""TeamMembers"" DROP COLUMN IF EXISTS ""DomainExpertise"";");

            migrationBuilder.Sql(@"ALTER TABLE ""Users"" DROP COLUMN IF EXISTS ""AppRoleId"";");

            migrationBuilder.Sql(@"DROP TABLE IF EXISTS ""RolePermissions"";");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS ""Permissions"";");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS ""AppRoles"";");
        }
    }
}
