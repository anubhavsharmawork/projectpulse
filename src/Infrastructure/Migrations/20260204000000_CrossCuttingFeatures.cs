using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260204000000_CrossCuttingFeatures")]
    public partial class CrossCuttingFeatures : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── TimeEntries — add IsBillable ──
            migrationBuilder.Sql(@"
                ALTER TABLE ""TimeEntries"" ADD COLUMN IF NOT EXISTS ""IsBillable"" boolean NOT NULL DEFAULT false;
            ");

            // ── Projects — add budget columns ──
            migrationBuilder.Sql(@"
                ALTER TABLE ""Projects"" ADD COLUMN IF NOT EXISTS ""EstimatedCost"" numeric(18,2) NOT NULL DEFAULT 0;
                ALTER TABLE ""Projects"" ADD COLUMN IF NOT EXISTS ""ActualCost""    numeric(18,2) NOT NULL DEFAULT 0;
            ");

            // ── WorkItems — add budget columns ──
            migrationBuilder.Sql(@"
                ALTER TABLE ""WorkItems"" ADD COLUMN IF NOT EXISTS ""EstimatedCost"" numeric(18,2) NOT NULL DEFAULT 0;
                ALTER TABLE ""WorkItems"" ADD COLUMN IF NOT EXISTS ""ActualCost""    numeric(18,2) NOT NULL DEFAULT 0;
            ");

            // ── AuditLogs ──
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS ""AuditLogs"" (
                    ""Id""          uuid NOT NULL PRIMARY KEY,
                    ""EntityType""  text NOT NULL DEFAULT '',
                    ""EntityId""    uuid NOT NULL,
                    ""Action""      text NOT NULL DEFAULT '',
                    ""OldValues""   text,
                    ""NewValues""   text,
                    ""UserId""      uuid,
                    ""Timestamp""   timestamp without time zone NOT NULL DEFAULT (now() at time zone 'utc')
                );
                CREATE INDEX IF NOT EXISTS ""IX_AuditLogs_EntityType""
                    ON ""AuditLogs"" (""EntityType"");
                CREATE INDEX IF NOT EXISTS ""IX_AuditLogs_Timestamp""
                    ON ""AuditLogs"" (""Timestamp"");
                CREATE INDEX IF NOT EXISTS ""IX_AuditLogs_EntityType_EntityId""
                    ON ""AuditLogs"" (""EntityType"", ""EntityId"");
            ");

            // ── Notifications ──
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS ""Notifications"" (
                    ""Id""                uuid NOT NULL PRIMARY KEY,
                    ""UserId""            uuid NOT NULL,
                    ""Type""              text NOT NULL DEFAULT '',
                    ""Message""           text NOT NULL DEFAULT '',
                    ""IsRead""            boolean NOT NULL DEFAULT false,
                    ""CreatedAt""         timestamp without time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    ""RelatedEntityId""   uuid
                );
                CREATE INDEX IF NOT EXISTS ""IX_Notifications_UserId""
                    ON ""Notifications"" (""UserId"");
                CREATE INDEX IF NOT EXISTS ""IX_Notifications_UserId_IsRead""
                    ON ""Notifications"" (""UserId"", ""IsRead"");
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS ""Notifications"";");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS ""AuditLogs"";");

            migrationBuilder.Sql(@"ALTER TABLE ""WorkItems"" DROP COLUMN IF EXISTS ""ActualCost"";");
            migrationBuilder.Sql(@"ALTER TABLE ""WorkItems"" DROP COLUMN IF EXISTS ""EstimatedCost"";");
            migrationBuilder.Sql(@"ALTER TABLE ""Projects"" DROP COLUMN IF EXISTS ""ActualCost"";");
            migrationBuilder.Sql(@"ALTER TABLE ""Projects"" DROP COLUMN IF EXISTS ""EstimatedCost"";");
            migrationBuilder.Sql(@"ALTER TABLE ""TimeEntries"" DROP COLUMN IF EXISTS ""IsBillable"";");
        }
    }
}
