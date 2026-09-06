using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aizen.Modules.Vessel.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddVesselEngineCatalogRefs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "VesselBrandId",
                schema: "vessel",
                table: "vessel_specifications",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "VesselModelId",
                schema: "vessel",
                table: "vessel_specifications",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "EngineBrandId",
                schema: "vessel",
                table: "vessel_engines",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "EngineModelId",
                schema: "vessel",
                table: "vessel_engines",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VesselBrandId",
                schema: "vessel",
                table: "vessel_specifications");

            migrationBuilder.DropColumn(
                name: "VesselModelId",
                schema: "vessel",
                table: "vessel_specifications");

            migrationBuilder.DropColumn(
                name: "EngineBrandId",
                schema: "vessel",
                table: "vessel_engines");

            migrationBuilder.DropColumn(
                name: "EngineModelId",
                schema: "vessel",
                table: "vessel_engines");
        }
    }
}
