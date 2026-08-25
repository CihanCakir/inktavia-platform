using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Aizen.Modules.Notification.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationCampaigns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "CampaignId",
                schema: "notification",
                table: "notifications",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "notification_campaigns",
                schema: "notification",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Audience = table.Column<int>(type: "integer", nullable: false),
                    TargetMode = table.Column<int>(type: "integer", nullable: false),
                    SelectedRecipientIdsJson = table.Column<string>(type: "jsonb", nullable: true),
                    TemplateCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CustomContentJson = table.Column<string>(type: "jsonb", nullable: true),
                    ChannelsCsv = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TotalRecipients = table.Column<int>(type: "integer", nullable: false),
                    SentCount = table.Column<int>(type: "integer", nullable: false),
                    FailedCount = table.Column<int>(type: "integer", nullable: false),
                    ScheduledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_campaigns", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_notifications_CampaignId",
                schema: "notification",
                table: "notifications",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_notification_campaigns_CreatedAt",
                schema: "notification",
                table: "notification_campaigns",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_notification_campaigns_Status",
                schema: "notification",
                table: "notification_campaigns",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "notification_campaigns",
                schema: "notification");

            migrationBuilder.DropIndex(
                name: "IX_notifications_CampaignId",
                schema: "notification",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "CampaignId",
                schema: "notification",
                table: "notifications");
        }
    }
}
