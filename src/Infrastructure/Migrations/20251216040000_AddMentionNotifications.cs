using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20251216040000_AddMentionNotifications")]
    public partial class AddMentionNotifications : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add MentionedUserIds column to Comments table if it doesn't exist
            // Using a simple approach that ignores errors if column already exists
            migrationBuilder.Sql(@"
                ALTER TABLE ""Comments"" ADD COLUMN IF NOT EXISTS ""MentionedUserIds"" text NULL;
            ");

            // Create MentionNotifications table
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS ""MentionNotifications"" (
                    ""Id"" uuid NOT NULL,
                    ""UserId"" uuid NOT NULL,
                    ""CommentId"" uuid NOT NULL,
                    ""WorkItemId"" uuid NOT NULL,
                    ""MentionedByUserId"" uuid NOT NULL,
                    ""CommentBody"" text NOT NULL,
                    ""WorkItemTitle"" text NOT NULL,
                    ""MentionedByName"" text NOT NULL,
                    ""IsRead"" boolean NOT NULL DEFAULT false,
                    ""CreatedAt"" timestamp with time zone NOT NULL,
                    CONSTRAINT ""PK_MentionNotifications"" PRIMARY KEY (""Id"")
                );
            ");

            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_MentionNotifications_UserId"" 
                    ON ""MentionNotifications"" (""UserId"");
            ");

            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_MentionNotifications_UserId_IsRead"" 
                    ON ""MentionNotifications"" (""UserId"", ""IsRead"");
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS ""MentionNotifications"";");
            migrationBuilder.Sql(@"
                ALTER TABLE ""Comments"" DROP COLUMN IF EXISTS ""MentionedUserIds"";
            ");
        }
    }
}
