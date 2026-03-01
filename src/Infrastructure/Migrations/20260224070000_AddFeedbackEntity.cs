using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260224070000_AddFeedbackEntity")]
    public partial class AddFeedbackEntity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS ""Feedbacks"" (
                    ""Id""              uuid NOT NULL,
                    ""UserId""          uuid,
                    ""UserEmail""       text,
                    ""UserDisplayName"" text,
                    ""Message""         character varying(2000) NOT NULL,
                    ""ProcessedAt""     timestamp without time zone,
                    ""CreatedAt""       timestamp without time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    ""UpdatedAt""       timestamp without time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    ""CreatedBy""       text NOT NULL DEFAULT '',
                    ""IsActive""        boolean NOT NULL DEFAULT true,
                    CONSTRAINT ""PK_Feedbacks"" PRIMARY KEY (""Id"")
                );
            ");

            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_Feedbacks_UserId"" ON ""Feedbacks"" (""UserId"");
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS ""Feedbacks"";");
        }
    }
}
