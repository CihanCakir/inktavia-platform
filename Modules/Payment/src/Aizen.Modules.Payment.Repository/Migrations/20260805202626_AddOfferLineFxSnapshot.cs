using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aizen.Modules.Payment.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddOfferLineFxSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "FxAppliedRate",
                schema: "payment",
                table: "offer_line_economics_snapshots",
                type: "numeric(18,6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FxRateDate",
                schema: "payment",
                table: "offer_line_economics_snapshots",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FxResolvedUnitPrice",
                schema: "payment",
                table: "offer_line_economics_snapshots",
                type: "numeric(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FxSettlementCurrencyCode",
                schema: "payment",
                table: "offer_line_economics_snapshots",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FxSourceCurrencyCode",
                schema: "payment",
                table: "offer_line_economics_snapshots",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FxSourceUnitPrice",
                schema: "payment",
                table: "offer_line_economics_snapshots",
                type: "numeric(18,4)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FxAppliedRate",
                schema: "payment",
                table: "offer_line_economics_snapshots");

            migrationBuilder.DropColumn(
                name: "FxRateDate",
                schema: "payment",
                table: "offer_line_economics_snapshots");

            migrationBuilder.DropColumn(
                name: "FxResolvedUnitPrice",
                schema: "payment",
                table: "offer_line_economics_snapshots");

            migrationBuilder.DropColumn(
                name: "FxSettlementCurrencyCode",
                schema: "payment",
                table: "offer_line_economics_snapshots");

            migrationBuilder.DropColumn(
                name: "FxSourceCurrencyCode",
                schema: "payment",
                table: "offer_line_economics_snapshots");

            migrationBuilder.DropColumn(
                name: "FxSourceUnitPrice",
                schema: "payment",
                table: "offer_line_economics_snapshots");
        }
    }
}
