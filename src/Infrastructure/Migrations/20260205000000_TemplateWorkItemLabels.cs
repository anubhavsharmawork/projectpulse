using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260205000000_TemplateWorkItemLabels")]
    public partial class TemplateWorkItemLabels : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE ""DomainTemplates"" ADD COLUMN IF NOT EXISTS ""WorkItemTypeLabels"" text;
            ");

            // Backfill existing templates that were seeded before this column existed
            migrationBuilder.Sql(@"
                UPDATE ""DomainTemplates"" SET ""WorkItemTypeLabels"" = '{""1"":""Epic"",""2"":""User Story"",""3"":""Task"",""4"":""SubTask""}'
                WHERE ""DomainType"" = 'IT' AND ""WorkItemTypeLabels"" IS NULL;
            ");
            migrationBuilder.Sql(@"
                UPDATE ""DomainTemplates"" SET ""WorkItemTypeLabels"" = '{""1"":""Initiative"",""2"":""Action Item"",""3"":""Task"",""4"":""SubTask""}'
                WHERE ""DomainType"" = 'Healthcare' AND ""WorkItemTypeLabels"" IS NULL;
            ");
            migrationBuilder.Sql(@"
                UPDATE ""DomainTemplates"" SET ""WorkItemTypeLabels"" = '{""1"":""Operation"",""2"":""Action Plan"",""3"":""Task"",""4"":""SubTask""}'
                WHERE ""DomainType"" = 'PublicSafety' AND ""WorkItemTypeLabels"" IS NULL;
            ");
            migrationBuilder.Sql(@"
                UPDATE ""DomainTemplates"" SET ""WorkItemTypeLabels"" = '{""1"":""Phase"",""2"":""Activity"",""3"":""Punch Item"",""4"":""SubItem""}'
                WHERE ""DomainType"" = 'Construction' AND ""WorkItemTypeLabels"" IS NULL;
            ");
            migrationBuilder.Sql(@"
                UPDATE ""DomainTemplates"" SET ""WorkItemTypeLabels"" = '{""1"":""Program"",""2"":""Work Package"",""3"":""Task"",""4"":""SubTask""}'
                WHERE ""DomainType"" = 'Infrastructure' AND ""WorkItemTypeLabels"" IS NULL;
            ");
            migrationBuilder.Sql(@"
                UPDATE ""DomainTemplates"" SET ""WorkItemTypeLabels"" = '{""1"":""Program"",""2"":""Initiative"",""3"":""Task"",""4"":""SubTask""}'
                WHERE ""DomainType"" = 'EconomicDevelopment' AND ""WorkItemTypeLabels"" IS NULL;
            ");
            migrationBuilder.Sql(@"
                UPDATE ""DomainTemplates"" SET ""WorkItemTypeLabels"" = '{""1"":""Epic"",""2"":""Feature"",""3"":""Task"",""4"":""SubTask""}'
                WHERE ""DomainType"" = 'Technology' AND ""WorkItemTypeLabels"" IS NULL;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE ""DomainTemplates"" DROP COLUMN IF EXISTS ""WorkItemTypeLabels"";
            ");
        }
    }
}
