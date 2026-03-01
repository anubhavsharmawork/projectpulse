using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260203000000_WorkflowEngine")]
    public partial class WorkflowEngine : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── WorkflowStates — add new columns ──
            migrationBuilder.Sql(@"
                ALTER TABLE ""WorkflowStates"" ADD COLUMN IF NOT EXISTS ""Color""               text NOT NULL DEFAULT '#6B7280';
                ALTER TABLE ""WorkflowStates"" ADD COLUMN IF NOT EXISTS ""AllowedTransitions""   text;
                ALTER TABLE ""WorkflowStates"" ADD COLUMN IF NOT EXISTS ""RequiredFields""       text;
                ALTER TABLE ""WorkflowStates"" ADD COLUMN IF NOT EXISTS ""NotifyOnEntry""        boolean NOT NULL DEFAULT false;
            ");

            // ── WorkflowTransitions ──
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS ""WorkflowTransitions"" (
                    ""Id""                      uuid NOT NULL PRIMARY KEY,
                    ""WorkItemId""              uuid NOT NULL REFERENCES ""WorkItems""(""Id"") ON DELETE CASCADE,
                    ""FromStateId""             uuid NOT NULL REFERENCES ""WorkflowStates""(""Id"") ON DELETE RESTRICT,
                    ""ToStateId""               uuid NOT NULL REFERENCES ""WorkflowStates""(""Id"") ON DELETE RESTRICT,
                    ""TransitionedByUserId""    uuid NOT NULL REFERENCES ""Users""(""Id"") ON DELETE CASCADE,
                    ""Comment""                 text,
                    ""CreatedAt""               timestamp without time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    ""UpdatedAt""               timestamp without time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    ""CreatedBy""               text NOT NULL DEFAULT '',
                    ""IsActive""                boolean NOT NULL DEFAULT true
                );
                CREATE INDEX IF NOT EXISTS ""IX_WorkflowTransitions_WorkItemId""
                    ON ""WorkflowTransitions"" (""WorkItemId"");
            ");

            // ── ProjectCategories ──
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS ""ProjectCategories"" (
                    ""Id""                uuid NOT NULL PRIMARY KEY,
                    ""Name""              text NOT NULL DEFAULT '',
                    ""DomainType""        text NOT NULL DEFAULT '',
                    ""Description""       text,
                    ""DefaultTeamRoles""  text,
                    ""CreatedAt""         timestamp without time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    ""UpdatedAt""         timestamp without time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    ""CreatedBy""         text NOT NULL DEFAULT '',
                    ""IsActive""          boolean NOT NULL DEFAULT true
                );
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS ""ProjectCategories"";");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS ""WorkflowTransitions"";");

            migrationBuilder.Sql(@"ALTER TABLE ""WorkflowStates"" DROP COLUMN IF EXISTS ""NotifyOnEntry"";");
            migrationBuilder.Sql(@"ALTER TABLE ""WorkflowStates"" DROP COLUMN IF EXISTS ""RequiredFields"";");
            migrationBuilder.Sql(@"ALTER TABLE ""WorkflowStates"" DROP COLUMN IF EXISTS ""AllowedTransitions"";");
            migrationBuilder.Sql(@"ALTER TABLE ""WorkflowStates"" DROP COLUMN IF EXISTS ""Color"";");
        }
    }
}
