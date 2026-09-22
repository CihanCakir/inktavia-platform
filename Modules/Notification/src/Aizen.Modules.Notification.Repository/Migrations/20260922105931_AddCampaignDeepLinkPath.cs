using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aizen.Modules.Notification.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddCampaignDeepLinkPath : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeepLinkPath",
                schema: "notification",
                table: "notification_campaigns",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeepLinkPath",
                schema: "notification",
                table: "notification_campaigns");
        }
    }
}
