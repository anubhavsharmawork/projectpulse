using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiTenancy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "WorkItems",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "WorkflowTransitions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "WorkflowStates",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Workflows",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Users",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "TimeEntries",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Teams",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "TeamMembers",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Relations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Projects",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "ProjectRoles",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "ProjectCategories",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Notifications",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "MentionNotifications",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "MaintenanceRecords",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Feedbacks",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "CustomFieldValues",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "CustomFields",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Comments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "AuditLogs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Attachments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Assets",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "AssetHistoryEntries",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "AssetCheckouts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Subdomain = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Tier = table.Column<string>(type: "text", nullable: false),
                    MaxUsers = table.Column<int>(type: "integer", nullable: false),
                    MaxProjects = table.Column<int>(type: "integer", nullable: false),
                    MaxStorageBytes = table.Column<long>(type: "bigint", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    SubscriptionExpiresAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Settings = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkItems_TenantId",
                table: "WorkItems",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTransitions_TenantId",
                table: "WorkflowTransitions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowStates_TenantId",
                table: "WorkflowStates",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Workflows_TenantId",
                table: "Workflows",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_TenantId",
                table: "Users",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TimeEntries_TenantId",
                table: "TimeEntries",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_TenantId",
                table: "Teams",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMembers_TenantId",
                table: "TeamMembers",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Relations_TenantId",
                table: "Relations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_TenantId",
                table: "Projects",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectRoles_TenantId",
                table: "ProjectRoles",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectCategories_TenantId",
                table: "ProjectCategories",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_TenantId",
                table: "Notifications",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_MentionNotifications_TenantId",
                table: "MentionNotifications",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceRecords_TenantId",
                table: "MaintenanceRecords",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_TenantId",
                table: "Feedbacks",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomFieldValues_TenantId",
                table: "CustomFieldValues",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomFields_TenantId",
                table: "CustomFields",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_TenantId",
                table: "Comments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_TenantId",
                table: "AuditLogs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_TenantId",
                table: "Attachments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_TenantId",
                table: "Assets",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetHistoryEntries_TenantId",
                table: "AssetHistoryEntries",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetCheckouts_TenantId",
                table: "AssetCheckouts",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Subdomain",
                table: "Tenants",
                column: "Subdomain",
                unique: true);

            // Seed default tenant for backward compatibility
            var defaultTenantId = new Guid("00000000-0000-0000-0000-000000000001");
            migrationBuilder.Sql($@"
                INSERT INTO ""Tenants"" (""Id"", ""Name"", ""Subdomain"", ""Tier"", ""MaxUsers"", ""MaxProjects"", ""MaxStorageBytes"", ""IsActive"", ""CreatedAt"")
                VALUES ('{defaultTenantId}', 'Default Organization', 'default', 'Enterprise', -1, -1, -1, true, NOW())
                ON CONFLICT DO NOTHING;
            ");

            // Migrate all existing data to the default tenant
            var tables = new[] { "Users", "Projects", "WorkItems", "Comments", "Teams", "TeamMembers",
                "TimeEntries", "Notifications", "MentionNotifications", "CustomFields", "CustomFieldValues",
                "Feedbacks", "Workflows", "WorkflowStates", "WorkflowTransitions", "AuditLogs", "Attachments",
                "Relations", "ProjectRoles", "ProjectCategories", "Assets", "AssetCheckouts",
                "AssetHistoryEntries", "MaintenanceRecords" };
            foreach (var table in tables)
            {
                migrationBuilder.Sql($@"UPDATE ""{table}"" SET ""TenantId"" = '{defaultTenantId}' WHERE ""TenantId"" = '00000000-0000-0000-0000-000000000000';");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Tenants");

            migrationBuilder.DropIndex(
                name: "IX_WorkItems_TenantId",
                table: "WorkItems");

            migrationBuilder.DropIndex(
                name: "IX_WorkflowTransitions_TenantId",
                table: "WorkflowTransitions");

            migrationBuilder.DropIndex(
                name: "IX_WorkflowStates_TenantId",
                table: "WorkflowStates");

            migrationBuilder.DropIndex(
                name: "IX_Workflows_TenantId",
                table: "Workflows");

            migrationBuilder.DropIndex(
                name: "IX_Users_TenantId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_TimeEntries_TenantId",
                table: "TimeEntries");

            migrationBuilder.DropIndex(
                name: "IX_Teams_TenantId",
                table: "Teams");

            migrationBuilder.DropIndex(
                name: "IX_TeamMembers_TenantId",
                table: "TeamMembers");

            migrationBuilder.DropIndex(
                name: "IX_Relations_TenantId",
                table: "Relations");

            migrationBuilder.DropIndex(
                name: "IX_Projects_TenantId",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_ProjectRoles_TenantId",
                table: "ProjectRoles");

            migrationBuilder.DropIndex(
                name: "IX_ProjectCategories_TenantId",
                table: "ProjectCategories");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_TenantId",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_MentionNotifications_TenantId",
                table: "MentionNotifications");

            migrationBuilder.DropIndex(
                name: "IX_MaintenanceRecords_TenantId",
                table: "MaintenanceRecords");

            migrationBuilder.DropIndex(
                name: "IX_Feedbacks_TenantId",
                table: "Feedbacks");

            migrationBuilder.DropIndex(
                name: "IX_CustomFieldValues_TenantId",
                table: "CustomFieldValues");

            migrationBuilder.DropIndex(
                name: "IX_CustomFields_TenantId",
                table: "CustomFields");

            migrationBuilder.DropIndex(
                name: "IX_Comments_TenantId",
                table: "Comments");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_TenantId",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_Attachments_TenantId",
                table: "Attachments");

            migrationBuilder.DropIndex(
                name: "IX_Assets_TenantId",
                table: "Assets");

            migrationBuilder.DropIndex(
                name: "IX_AssetHistoryEntries_TenantId",
                table: "AssetHistoryEntries");

            migrationBuilder.DropIndex(
                name: "IX_AssetCheckouts_TenantId",
                table: "AssetCheckouts");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "WorkItems");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "WorkflowTransitions");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "WorkflowStates");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Workflows");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "TimeEntries");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "TeamMembers");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Relations");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "ProjectRoles");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "ProjectCategories");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "MentionNotifications");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "MaintenanceRecords");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Feedbacks");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "CustomFieldValues");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "CustomFields");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Attachments");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "AssetHistoryEntries");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "AssetCheckouts");
        }
    }
}
