using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aizen.Modules.ServiceRequest.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddCargoDrySupplyOrderModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AutoCompleteDeadlineUtc",
                schema: "servicerequest",
                table: "service_requests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CargoDryRetailAmount",
                schema: "servicerequest",
                table: "service_requests",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CargoDryRetailCurrency",
                schema: "servicerequest",
                table: "service_requests",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveredAtUtc",
                schema: "servicerequest",
                table: "service_requests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeliveredKitId",
                schema: "servicerequest",
                table: "service_requests",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProviderAcceptDeadlineUtc",
                schema: "servicerequest",
                table: "service_requests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ShippedAtUtc",
                schema: "servicerequest",
                table: "service_requests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TrackingCode",
                schema: "servicerequest",
                table: "service_requests",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_service_requests_Status_AutoCompleteDeadline",
                schema: "servicerequest",
                table: "service_requests",
                columns: new[] { "Status", "AutoCompleteDeadlineUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_service_requests_Status_ProviderAcceptDeadline",
                schema: "servicerequest",
                table: "service_requests",
                columns: new[] { "Status", "ProviderAcceptDeadlineUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_service_requests_Status_AutoCompleteDeadline",
                schema: "servicerequest",
                table: "service_requests");

            migrationBuilder.DropIndex(
                name: "IX_service_requests_Status_ProviderAcceptDeadline",
                schema: "servicerequest",
                table: "service_requests");

            migrationBuilder.DropColumn(
                name: "AutoCompleteDeadlineUtc",
                schema: "servicerequest",
                table: "service_requests");

            migrationBuilder.DropColumn(
                name: "CargoDryRetailAmount",
                schema: "servicerequest",
                table: "service_requests");

            migrationBuilder.DropColumn(
                name: "CargoDryRetailCurrency",
                schema: "servicerequest",
                table: "service_requests");

            migrationBuilder.DropColumn(
                name: "DeliveredAtUtc",
                schema: "servicerequest",
                table: "service_requests");

            migrationBuilder.DropColumn(
                name: "DeliveredKitId",
                schema: "servicerequest",
                table: "service_requests");

            migrationBuilder.DropColumn(
                name: "ProviderAcceptDeadlineUtc",
                schema: "servicerequest",
                table: "service_requests");

            migrationBuilder.DropColumn(
                name: "ShippedAtUtc",
                schema: "servicerequest",
                table: "service_requests");

            migrationBuilder.DropColumn(
                name: "TrackingCode",
                schema: "servicerequest",
                table: "service_requests");
        }
    }
}
