using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aizen.Modules.CargoDry.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddMonthlySettlementGroupingIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "SettlementCode",
                schema: "cargodry",
                table: "sell_through_settlements",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.CreateIndex(
                name: "IX_sell_through_settlements_CurrencyCode",
                schema: "cargodry",
                table: "sell_through_settlements",
                column: "CurrencyCode");

            migrationBuilder.CreateIndex(
                name: "IX_sell_through_settlements_ProviderProfileId_CurrencyCode_Pro~",
                schema: "cargodry",
                table: "sell_through_settlements",
                columns: new[] { "ProviderProfileId", "CurrencyCode", "ProductCode", "PeriodStartUtc", "PeriodEndUtc", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_sell_through_settlements_CurrencyCode",
                schema: "cargodry",
                table: "sell_through_settlements");

            migrationBuilder.DropIndex(
                name: "IX_sell_through_settlements_ProviderProfileId_CurrencyCode_Pro~",
                schema: "cargodry",
                table: "sell_through_settlements");

            migrationBuilder.AlterColumn<string>(
                name: "SettlementCode",
                schema: "cargodry",
                table: "sell_through_settlements",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);
        }
    }
}
