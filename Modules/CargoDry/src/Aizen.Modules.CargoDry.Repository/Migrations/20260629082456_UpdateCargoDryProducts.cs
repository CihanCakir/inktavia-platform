using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aizen.Modules.CargoDry.Repository.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCargoDryProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeviceType",
                schema: "cargodry",
                table: "products",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BatchLabel",
                schema: "cargodry",
                table: "batches",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductionNotes",
                schema: "cargodry",
                table: "batches",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WarehouseCode",
                schema: "cargodry",
                table: "batches",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeviceType",
                schema: "cargodry",
                table: "products");

            migrationBuilder.DropColumn(
                name: "BatchLabel",
                schema: "cargodry",
                table: "batches");

            migrationBuilder.DropColumn(
                name: "ProductionNotes",
                schema: "cargodry",
                table: "batches");

            migrationBuilder.DropColumn(
                name: "WarehouseCode",
                schema: "cargodry",
                table: "batches");
        }
    }
}
