using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aizen.Modules.Payment.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddPayoutRecordLifecycleFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAtUtc",
                schema: "payment",
                table: "payout_records",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ApprovedByUserId",
                schema: "payment",
                table: "payout_records",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CompletedByUserId",
                schema: "payment",
                table: "payout_records",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FailedAtUtc",
                schema: "payment",
                table: "payout_records",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "FailedByUserId",
                schema: "payment",
                table: "payout_records",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProcessingAtUtc",
                schema: "payment",
                table: "payout_records",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ProcessingByUserId",
                schema: "payment",
                table: "payout_records",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovedAtUtc",
                schema: "payment",
                table: "payout_records");

            migrationBuilder.DropColumn(
                name: "ApprovedByUserId",
                schema: "payment",
                table: "payout_records");

            migrationBuilder.DropColumn(
                name: "CompletedByUserId",
                schema: "payment",
                table: "payout_records");

            migrationBuilder.DropColumn(
                name: "FailedAtUtc",
                schema: "payment",
                table: "payout_records");

            migrationBuilder.DropColumn(
                name: "FailedByUserId",
                schema: "payment",
                table: "payout_records");

            migrationBuilder.DropColumn(
                name: "ProcessingAtUtc",
                schema: "payment",
                table: "payout_records");

            migrationBuilder.DropColumn(
                name: "ProcessingByUserId",
                schema: "payment",
                table: "payout_records");
        }
    }
}
