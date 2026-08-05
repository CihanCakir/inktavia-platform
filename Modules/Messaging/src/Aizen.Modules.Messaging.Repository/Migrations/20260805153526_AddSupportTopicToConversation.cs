using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aizen.Modules.Messaging.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddSupportTopicToConversation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Topic",
                schema: "messaging",
                table: "conversations",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_conversations_Topic",
                schema: "messaging",
                table: "conversations",
                column: "Topic");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_conversations_Topic",
                schema: "messaging",
                table: "conversations");

            migrationBuilder.DropColumn(
                name: "Topic",
                schema: "messaging",
                table: "conversations");
        }
    }
}
