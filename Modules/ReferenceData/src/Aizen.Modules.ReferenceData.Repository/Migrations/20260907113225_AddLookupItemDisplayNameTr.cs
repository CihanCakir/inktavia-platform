using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aizen.Modules.ReferenceData.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddLookupItemDisplayNameTr : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DisplayNameTr",
                schema: "ref",
                table: "LookupItems",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisplayNameTr",
                schema: "ref",
                table: "LookupItems");
        }
    }
}
