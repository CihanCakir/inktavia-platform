using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aizen.Modules.ReferenceData.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddMarinaIsAdminEdited : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAdminEdited",
                schema: "ref",
                table: "Marinas",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAdminEdited",
                schema: "ref",
                table: "Marinas");
        }
    }
}
