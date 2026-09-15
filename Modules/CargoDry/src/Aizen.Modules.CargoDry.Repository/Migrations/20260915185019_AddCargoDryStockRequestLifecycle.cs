using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aizen.Modules.CargoDry.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddCargoDryStockRequestLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AutoReceiveDeadlineUtc",
                schema: "cargodry",
                table: "cargodry_stock_requests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReceivedAtUtc",
                schema: "cargodry",
                table: "cargodry_stock_requests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ReceivedByUserId",
                schema: "cargodry",
                table: "cargodry_stock_requests",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ShippedAtUtc",
                schema: "cargodry",
                table: "cargodry_stock_requests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ShippedByUserId",
                schema: "cargodry",
                table: "cargodry_stock_requests",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TrackingCode",
                schema: "cargodry",
                table: "cargodry_stock_requests",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_cargodry_stock_requests_Status_AutoReceiveDeadlineUtc",
                schema: "cargodry",
                table: "cargodry_stock_requests",
                columns: new[] { "Status", "AutoReceiveDeadlineUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_cargodry_stock_requests_Status_AutoReceiveDeadlineUtc",
                schema: "cargodry",
                table: "cargodry_stock_requests");

            migrationBuilder.DropColumn(
                name: "AutoReceiveDeadlineUtc",
                schema: "cargodry",
                table: "cargodry_stock_requests");

            migrationBuilder.DropColumn(
                name: "ReceivedAtUtc",
                schema: "cargodry",
                table: "cargodry_stock_requests");

            migrationBuilder.DropColumn(
                name: "ReceivedByUserId",
                schema: "cargodry",
                table: "cargodry_stock_requests");

            migrationBuilder.DropColumn(
                name: "ShippedAtUtc",
                schema: "cargodry",
                table: "cargodry_stock_requests");

            migrationBuilder.DropColumn(
                name: "ShippedByUserId",
                schema: "cargodry",
                table: "cargodry_stock_requests");

            migrationBuilder.DropColumn(
                name: "TrackingCode",
                schema: "cargodry",
                table: "cargodry_stock_requests");
        }
    }
}
