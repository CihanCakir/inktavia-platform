using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aizen.Modules.ServiceRequest.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddDisputeResolutionOutcome : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PaymentOutcomeAppliedAt",
                schema: "servicerequest",
                table: "service_request_disputes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ResolutionOutcome",
                schema: "servicerequest",
                table: "service_request_disputes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ResolutionRefundAmount",
                schema: "servicerequest",
                table: "service_request_disputes",
                type: "numeric(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentOutcomeAppliedAt",
                schema: "servicerequest",
                table: "service_request_disputes");

            migrationBuilder.DropColumn(
                name: "ResolutionOutcome",
                schema: "servicerequest",
                table: "service_request_disputes");

            migrationBuilder.DropColumn(
                name: "ResolutionRefundAmount",
                schema: "servicerequest",
                table: "service_request_disputes");
        }
    }
}
