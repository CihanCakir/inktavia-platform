using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aizen.Modules.ReferenceData.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogMergedIntoId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "MergedIntoId",
                schema: "ref",
                table: "VesselModels",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "MergedIntoId",
                schema: "ref",
                table: "VesselBrands",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "MergedIntoId",
                schema: "ref",
                table: "EngineModels",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "MergedIntoId",
                schema: "ref",
                table: "EngineBrands",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MergedIntoId",
                schema: "ref",
                table: "VesselModels");

            migrationBuilder.DropColumn(
                name: "MergedIntoId",
                schema: "ref",
                table: "VesselBrands");

            migrationBuilder.DropColumn(
                name: "MergedIntoId",
                schema: "ref",
                table: "EngineModels");

            migrationBuilder.DropColumn(
                name: "MergedIntoId",
                schema: "ref",
                table: "EngineBrands");
        }
    }
}
