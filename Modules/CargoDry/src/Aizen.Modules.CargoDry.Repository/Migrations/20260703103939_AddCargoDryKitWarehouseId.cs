using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aizen.Modules.CargoDry.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddCargoDryKitWarehouseId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "WarehouseId",
                schema: "cargodry",
                table: "kits",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_kits_WarehouseId",
                schema: "cargodry",
                table: "kits",
                column: "WarehouseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_kits_WarehouseId",
                schema: "cargodry",
                table: "kits");

            migrationBuilder.DropColumn(
                name: "WarehouseId",
                schema: "cargodry",
                table: "kits");
        }
    }
}
