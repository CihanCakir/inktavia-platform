using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aizen.Modules.Notification.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationReference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReferenceType",
                schema: "notification",
                table: "notifications",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ReferenceId",
                schema: "notification",
                table: "notifications",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReferenceType",
                schema: "notification",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "ReferenceId",
                schema: "notification",
                table: "notifications");
        }
    }
}
