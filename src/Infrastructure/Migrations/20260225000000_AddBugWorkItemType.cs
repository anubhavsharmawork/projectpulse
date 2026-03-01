using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260225000000_AddBugWorkItemType")]
    public partial class AddBugWorkItemType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE ""WorkItems"" ADD COLUMN IF NOT EXISTS ""Severity"" integer;
                ALTER TABLE ""WorkItems"" ADD COLUMN IF NOT EXISTS ""StepsToReproduce"" text;
                ALTER TABLE ""WorkItems"" ADD COLUMN IF NOT EXISTS ""ExpectedBehavior"" text;
                ALTER TABLE ""WorkItems"" ADD COLUMN IF NOT EXISTS ""ActualBehavior"" text;
                ALTER TABLE ""WorkItems"" ADD COLUMN IF NOT EXISTS ""Environment"" text;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE ""WorkItems"" DROP COLUMN IF EXISTS ""Severity"";
                ALTER TABLE ""WorkItems"" DROP COLUMN IF EXISTS ""StepsToReproduce"";
                ALTER TABLE ""WorkItems"" DROP COLUMN IF EXISTS ""ExpectedBehavior"";
                ALTER TABLE ""WorkItems"" DROP COLUMN IF EXISTS ""ActualBehavior"";
                ALTER TABLE ""WorkItems"" DROP COLUMN IF EXISTS ""Environment"";
            ");
        }
    }
}
