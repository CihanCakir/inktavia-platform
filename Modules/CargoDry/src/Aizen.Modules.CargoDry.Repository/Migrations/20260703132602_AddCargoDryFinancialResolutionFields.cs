using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aizen.Modules.CargoDry.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddCargoDryFinancialResolutionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ReadyForSettlementAtUtc",
                schema: "cargodry",
                table: "sell_through_settlements",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FinancialResolvedAtUtc",
                schema: "cargodry",
                table: "sales_attributions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "FinancialResolvedByUserId",
                schema: "cargodry",
                table: "sales_attributions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PlatformShareAmount",
                schema: "cargodry",
                table: "sales_attributions",
                type: "numeric(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ProviderShareAmount",
                schema: "cargodry",
                table: "sales_attributions",
                type: "numeric(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResolutionNote",
                schema: "cargodry",
                table: "sales_attributions",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReadyForSettlementAtUtc",
                schema: "cargodry",
                table: "sell_through_settlements");

            migrationBuilder.DropColumn(
                name: "FinancialResolvedAtUtc",
                schema: "cargodry",
                table: "sales_attributions");

            migrationBuilder.DropColumn(
                name: "FinancialResolvedByUserId",
                schema: "cargodry",
                table: "sales_attributions");

            migrationBuilder.DropColumn(
                name: "PlatformShareAmount",
                schema: "cargodry",
                table: "sales_attributions");

            migrationBuilder.DropColumn(
                name: "ProviderShareAmount",
                schema: "cargodry",
                table: "sales_attributions");

            migrationBuilder.DropColumn(
                name: "ResolutionNote",
                schema: "cargodry",
                table: "sales_attributions");
        }
    }
}
