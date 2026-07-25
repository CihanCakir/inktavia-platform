using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aizen.Modules.CargoDry.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddCargoDrySalesAttributionTierSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TierAtSale",
                schema: "cargodry",
                table: "sales_attributions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TierBonusRate",
                schema: "cargodry",
                table: "sales_attributions",
                type: "numeric(8,4)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TierAtSale",
                schema: "cargodry",
                table: "sales_attributions");

            migrationBuilder.DropColumn(
                name: "TierBonusRate",
                schema: "cargodry",
                table: "sales_attributions");
        }
    }
}
