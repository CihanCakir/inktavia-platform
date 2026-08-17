using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aizen.Modules.Notification.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddWebPushSubscriptionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AuthKey",
                schema: "notification",
                table: "user_device_tokens",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Endpoint",
                schema: "notification",
                table: "user_device_tokens",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "P256dhKey",
                schema: "notification",
                table: "user_device_tokens",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_device_tokens_Endpoint",
                schema: "notification",
                table: "user_device_tokens",
                column: "Endpoint",
                unique: true,
                filter: "\"Endpoint\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_user_device_tokens_Endpoint",
                schema: "notification",
                table: "user_device_tokens");

            migrationBuilder.DropColumn(
                name: "AuthKey",
                schema: "notification",
                table: "user_device_tokens");

            migrationBuilder.DropColumn(
                name: "Endpoint",
                schema: "notification",
                table: "user_device_tokens");

            migrationBuilder.DropColumn(
                name: "P256dhKey",
                schema: "notification",
                table: "user_device_tokens");
        }
    }
}
