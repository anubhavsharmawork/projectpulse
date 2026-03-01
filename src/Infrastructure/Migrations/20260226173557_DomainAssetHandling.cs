using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DomainAssetHandling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Capacity",
                table: "Assets",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Category",
                table: "Assets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "DomainAssetConfigId",
                table: "Assets",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GridReference",
                table: "Assets",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LicenseExpiryDate",
                table: "Assets",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LicenseKey",
                table: "Assets",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LicensedSeats",
                table: "Assets",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegulatoryId",
                table: "Assets",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Vendor",
                table: "Assets",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DomainAssetConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DomainType = table.Column<string>(type: "text", nullable: false),
                    AssetType = table.Column<int>(type: "integer", nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    DisplayLabel = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    DefaultDepreciationMethod = table.Column<int>(type: "integer", nullable: false),
                    DefaultUsefulLifeYears = table.Column<int>(type: "integer", nullable: false),
                    DefaultMaintenanceIntervalDays = table.Column<int>(type: "integer", nullable: true),
                    ComplianceNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    DefaultFields = table.Column<string>(type: "text", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DomainAssetConfigs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Assets_DomainAssetConfigId",
                table: "Assets",
                column: "DomainAssetConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_DomainAssetConfigs_DomainType_AssetType",
                table: "DomainAssetConfigs",
                columns: new[] { "DomainType", "AssetType" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Assets_DomainAssetConfigs_DomainAssetConfigId",
                table: "Assets",
                column: "DomainAssetConfigId",
                principalTable: "DomainAssetConfigs",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assets_DomainAssetConfigs_DomainAssetConfigId",
                table: "Assets");

            migrationBuilder.DropTable(
                name: "DomainAssetConfigs");

            migrationBuilder.DropIndex(
                name: "IX_Assets_DomainAssetConfigId",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "Capacity",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "DomainAssetConfigId",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "GridReference",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "LicenseExpiryDate",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "LicenseKey",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "LicensedSeats",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "RegulatoryId",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "Vendor",
                table: "Assets");
        }
    }
}
