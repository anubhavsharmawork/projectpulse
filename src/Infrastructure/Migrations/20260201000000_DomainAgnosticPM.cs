using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260201000000_DomainAgnosticPM")]
    public partial class DomainAgnosticPM : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Workflows ──
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS ""Workflows"" (
                    ""Id""          uuid NOT NULL PRIMARY KEY,
                    ""Name""        text NOT NULL DEFAULT '',
                    ""DomainType""  text NOT NULL DEFAULT '',
                    ""CreatedAt""   timestamp without time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    ""UpdatedAt""   timestamp without time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    ""CreatedBy""   text NOT NULL DEFAULT '',
                    ""IsActive""    boolean NOT NULL DEFAULT true
                );
            ");

            // ── WorkflowStates ──
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS ""WorkflowStates"" (
                    ""Id""          uuid NOT NULL PRIMARY KEY,
                    ""WorkflowId""  uuid NOT NULL REFERENCES ""Workflows""(""Id"") ON DELETE CASCADE,
                    ""Name""        text NOT NULL DEFAULT '',
                    ""Order""       integer NOT NULL DEFAULT 0,
                    ""IsInitial""   boolean NOT NULL DEFAULT false,
                    ""IsFinal""     boolean NOT NULL DEFAULT false,
                    ""CreatedAt""   timestamp without time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    ""UpdatedAt""   timestamp without time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    ""CreatedBy""   text NOT NULL DEFAULT '',
                    ""IsActive""    boolean NOT NULL DEFAULT true
                );
            ");

            // ── DomainTemplates ──
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS ""DomainTemplates"" (
                    ""Id""                        uuid NOT NULL PRIMARY KEY,
                    ""Name""                      text NOT NULL DEFAULT '',
                    ""DomainType""                text NOT NULL DEFAULT '',
                    ""DefaultNotificationRules""  text,
                    ""DefaultWorkflowId""         uuid REFERENCES ""Workflows""(""Id"") ON DELETE SET NULL,
                    ""CreatedAt""                 timestamp without time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    ""UpdatedAt""                 timestamp without time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    ""CreatedBy""                 text NOT NULL DEFAULT '',
                    ""IsActive""                  boolean NOT NULL DEFAULT true
                );
            ");

            // ── CustomFields ──
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS ""CustomFields"" (
                    ""Id""                uuid NOT NULL PRIMARY KEY,
                    ""Name""              text NOT NULL DEFAULT '',
                    ""FieldType""         text NOT NULL DEFAULT '',
                    ""DomainType""        text NOT NULL DEFAULT '',
                    ""IsRequired""        boolean NOT NULL DEFAULT false,
                    ""Options""           text,
                    ""ValidationRule""    text,
                    ""DomainTemplateId""  uuid REFERENCES ""DomainTemplates""(""Id"") ON DELETE SET NULL,
                    ""CreatedAt""         timestamp without time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    ""UpdatedAt""         timestamp without time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    ""CreatedBy""         text NOT NULL DEFAULT '',
                    ""IsActive""          boolean NOT NULL DEFAULT true
                );
            ");

            // ── CustomFieldValues ──
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS ""CustomFieldValues"" (
                    ""Id""              uuid NOT NULL PRIMARY KEY,
                    ""CustomFieldId""   uuid NOT NULL REFERENCES ""CustomFields""(""Id"") ON DELETE CASCADE,
                    ""EntityId""        uuid NOT NULL,
                    ""EntityType""      text NOT NULL DEFAULT '',
                    ""Value""           text,
                    ""CreatedAt""       timestamp without time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    ""UpdatedAt""       timestamp without time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    ""CreatedBy""       text NOT NULL DEFAULT '',
                    ""IsActive""        boolean NOT NULL DEFAULT true
                );
                CREATE INDEX IF NOT EXISTS ""IX_CustomFieldValues_EntityId_CustomFieldId""
                    ON ""CustomFieldValues"" (""EntityId"", ""CustomFieldId"");
            ");

            // ── Teams ──
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS ""Teams"" (
                    ""Id""          uuid NOT NULL PRIMARY KEY,
                    ""Name""        text NOT NULL DEFAULT '',
                    ""ProjectId""   uuid NOT NULL REFERENCES ""Projects""(""Id"") ON DELETE CASCADE,
                    ""CreatedAt""   timestamp without time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    ""UpdatedAt""   timestamp without time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    ""CreatedBy""   text NOT NULL DEFAULT '',
                    ""IsActive""    boolean NOT NULL DEFAULT true
                );
            ");

            // ── TeamMembers ──
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS ""TeamMembers"" (
                    ""Id""          uuid NOT NULL PRIMARY KEY,
                    ""TeamId""      uuid NOT NULL REFERENCES ""Teams""(""Id"") ON DELETE CASCADE,
                    ""UserId""      uuid NOT NULL REFERENCES ""Users""(""Id"") ON DELETE CASCADE,
                    ""Role""        text NOT NULL DEFAULT '',
                    ""CreatedAt""   timestamp without time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    ""UpdatedAt""   timestamp without time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    ""CreatedBy""   text NOT NULL DEFAULT '',
                    ""IsActive""    boolean NOT NULL DEFAULT true
                );
            ");

            // ── Attachments ──
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS ""Attachments"" (
                    ""Id""            uuid NOT NULL PRIMARY KEY,
                    ""WorkItemId""    uuid NOT NULL REFERENCES ""WorkItems""(""Id"") ON DELETE CASCADE,
                    ""FileName""      text NOT NULL DEFAULT '',
                    ""StorageUrl""    text NOT NULL DEFAULT '',
                    ""ContentType""   text NOT NULL DEFAULT '',
                    ""SizeBytes""     bigint NOT NULL DEFAULT 0,
                    ""CreatedAt""     timestamp without time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    ""UpdatedAt""     timestamp without time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    ""CreatedBy""     text NOT NULL DEFAULT '',
                    ""IsActive""      boolean NOT NULL DEFAULT true
                );
            ");

            // ── TimeEntries ──
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS ""TimeEntries"" (
                    ""Id""            uuid NOT NULL PRIMARY KEY,
                    ""WorkItemId""    uuid NOT NULL REFERENCES ""WorkItems""(""Id"") ON DELETE CASCADE,
                    ""UserId""        uuid NOT NULL REFERENCES ""Users""(""Id"") ON DELETE CASCADE,
                    ""Hours""         numeric(10,2) NOT NULL DEFAULT 0,
                    ""Description""   text,
                    ""LoggedDate""    timestamp without time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    ""CreatedAt""     timestamp without time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    ""UpdatedAt""     timestamp without time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    ""CreatedBy""     text NOT NULL DEFAULT '',
                    ""IsActive""      boolean NOT NULL DEFAULT true
                );
            ");

            // ── Relations ──
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS ""Relations"" (
                    ""Id""                    uuid NOT NULL PRIMARY KEY,
                    ""SourceWorkItemId""      uuid NOT NULL REFERENCES ""WorkItems""(""Id"") ON DELETE RESTRICT,
                    ""TargetWorkItemId""      uuid NOT NULL REFERENCES ""WorkItems""(""Id"") ON DELETE RESTRICT,
                    ""RelationType""          text NOT NULL DEFAULT '',
                    ""CreatedAt""             timestamp without time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    ""UpdatedAt""             timestamp without time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    ""CreatedBy""             text NOT NULL DEFAULT '',
                    ""IsActive""              boolean NOT NULL DEFAULT true
                );
                CREATE INDEX IF NOT EXISTS ""IX_Relations_Source_Target""
                    ON ""Relations"" (""SourceWorkItemId"", ""TargetWorkItemId"");
            ");

            // ── Alter Projects — add domain-agnostic columns ──
            migrationBuilder.Sql(@"
                ALTER TABLE ""Projects"" ADD COLUMN IF NOT EXISTS ""UpdatedAt""    timestamp without time zone NOT NULL DEFAULT (now() at time zone 'utc');
                ALTER TABLE ""Projects"" ADD COLUMN IF NOT EXISTS ""CreatedBy""    text NOT NULL DEFAULT '';
                ALTER TABLE ""Projects"" ADD COLUMN IF NOT EXISTS ""IsActive""     boolean NOT NULL DEFAULT true;
                ALTER TABLE ""Projects"" ADD COLUMN IF NOT EXISTS ""DomainType""   text NOT NULL DEFAULT 'IT';
                ALTER TABLE ""Projects"" ADD COLUMN IF NOT EXISTS ""TemplateId""   uuid REFERENCES ""DomainTemplates""(""Id"") ON DELETE SET NULL;
                ALTER TABLE ""Projects"" ADD COLUMN IF NOT EXISTS ""WorkflowId""   uuid REFERENCES ""Workflows""(""Id"") ON DELETE SET NULL;
            ");

            // ── Alter WorkItems — add new columns ──
            migrationBuilder.Sql(@"
                ALTER TABLE ""WorkItems"" ADD COLUMN IF NOT EXISTS ""UpdatedAt""       timestamp without time zone NOT NULL DEFAULT (now() at time zone 'utc');
                ALTER TABLE ""WorkItems"" ADD COLUMN IF NOT EXISTS ""CreatedBy""       text NOT NULL DEFAULT '';
                ALTER TABLE ""WorkItems"" ADD COLUMN IF NOT EXISTS ""IsActive""        boolean NOT NULL DEFAULT true;
                ALTER TABLE ""WorkItems"" ADD COLUMN IF NOT EXISTS ""CurrentStateId""  uuid REFERENCES ""WorkflowStates""(""Id"") ON DELETE SET NULL;
            ");

            // ── Alter Comments — add new columns ──
            migrationBuilder.Sql(@"
                ALTER TABLE ""Comments"" ADD COLUMN IF NOT EXISTS ""UpdatedAt""  timestamp without time zone NOT NULL DEFAULT (now() at time zone 'utc');
                ALTER TABLE ""Comments"" ADD COLUMN IF NOT EXISTS ""IsActive""   boolean NOT NULL DEFAULT true;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE ""Comments"" DROP COLUMN IF EXISTS ""IsActive"";");
            migrationBuilder.Sql(@"ALTER TABLE ""Comments"" DROP COLUMN IF EXISTS ""UpdatedAt"";");

            migrationBuilder.Sql(@"ALTER TABLE ""WorkItems"" DROP COLUMN IF EXISTS ""CurrentStateId"";");
            migrationBuilder.Sql(@"ALTER TABLE ""WorkItems"" DROP COLUMN IF EXISTS ""IsActive"";");
            migrationBuilder.Sql(@"ALTER TABLE ""WorkItems"" DROP COLUMN IF EXISTS ""CreatedBy"";");
            migrationBuilder.Sql(@"ALTER TABLE ""WorkItems"" DROP COLUMN IF EXISTS ""UpdatedAt"";");

            migrationBuilder.Sql(@"ALTER TABLE ""Projects"" DROP COLUMN IF EXISTS ""WorkflowId"";");
            migrationBuilder.Sql(@"ALTER TABLE ""Projects"" DROP COLUMN IF EXISTS ""TemplateId"";");
            migrationBuilder.Sql(@"ALTER TABLE ""Projects"" DROP COLUMN IF EXISTS ""DomainType"";");
            migrationBuilder.Sql(@"ALTER TABLE ""Projects"" DROP COLUMN IF EXISTS ""IsActive"";");
            migrationBuilder.Sql(@"ALTER TABLE ""Projects"" DROP COLUMN IF EXISTS ""CreatedBy"";");
            migrationBuilder.Sql(@"ALTER TABLE ""Projects"" DROP COLUMN IF EXISTS ""UpdatedAt"";");

            migrationBuilder.Sql(@"DROP TABLE IF EXISTS ""Relations"";");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS ""TimeEntries"";");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS ""Attachments"";");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS ""TeamMembers"";");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS ""Teams"";");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS ""CustomFieldValues"";");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS ""CustomFields"";");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS ""DomainTemplates"";");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS ""WorkflowStates"";");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS ""Workflows"";");
        }
    }
}
