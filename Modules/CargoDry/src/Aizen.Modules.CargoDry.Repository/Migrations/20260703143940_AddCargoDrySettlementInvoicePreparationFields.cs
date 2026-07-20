using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aizen.Modules.CargoDry.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddCargoDrySettlementInvoicePreparationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "InvoiceId",
                schema: "cargodry",
                table: "sell_through_settlements",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvoicePreparationNote",
                schema: "cargodry",
                table: "sell_through_settlements",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "InvoicePreparedAtUtc",
                schema: "cargodry",
                table: "sell_through_settlements",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "InvoicePreparedByUserId",
                schema: "cargodry",
                table: "sell_through_settlements",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_sell_through_settlements_InvoiceId",
                schema: "cargodry",
                table: "sell_through_settlements",
                column: "InvoiceId",
                filter: "\"InvoiceId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_sell_through_settlements_InvoiceId",
                schema: "cargodry",
                table: "sell_through_settlements");

            migrationBuilder.DropColumn(
                name: "InvoiceId",
                schema: "cargodry",
                table: "sell_through_settlements");

            migrationBuilder.DropColumn(
                name: "InvoicePreparationNote",
                schema: "cargodry",
                table: "sell_through_settlements");

            migrationBuilder.DropColumn(
                name: "InvoicePreparedAtUtc",
                schema: "cargodry",
                table: "sell_through_settlements");

            migrationBuilder.DropColumn(
                name: "InvoicePreparedByUserId",
                schema: "cargodry",
                table: "sell_through_settlements");
        }
    }
}
