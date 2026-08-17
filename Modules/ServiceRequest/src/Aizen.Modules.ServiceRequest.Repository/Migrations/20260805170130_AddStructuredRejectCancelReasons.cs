using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aizen.Modules.ServiceRequest.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddStructuredRejectCancelReasons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CancelReasonCode",
                schema: "servicerequest",
                table: "service_requests",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RejectReasonCode",
                schema: "servicerequest",
                table: "service_request_offers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RejectReasonCode",
                schema: "servicerequest",
                table: "service_request_completions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RejectReasonCode",
                schema: "servicerequest",
                table: "service_request_assignments",
                type: "integer",
                nullable: true);

            // N-E backfill — existing reject/cancel rows predate the taxonomy: set the structured code to Other (99)
            // while preserving the original free-text in its column (the ReasonNote). Only touch rows that were
            // actually cancelled/rejected; rows never rejected keep a NULL code.
            migrationBuilder.Sql(
                "UPDATE servicerequest.service_requests SET \"CancelReasonCode\" = 99 " +
                "WHERE \"CancelledAt\" IS NOT NULL AND \"CancelReasonCode\" IS NULL;");
            migrationBuilder.Sql(
                "UPDATE servicerequest.service_request_offers SET \"RejectReasonCode\" = 99 " +
                "WHERE \"Status\" = 5 AND \"RejectReasonCode\" IS NULL;");        // OfferStatus.Rejected
            migrationBuilder.Sql(
                "UPDATE servicerequest.service_request_assignments SET \"RejectReasonCode\" = 99 " +
                "WHERE \"Status\" = 3 AND \"RejectReasonCode\" IS NULL;");        // AssignmentStatus.Rejected
            migrationBuilder.Sql(
                "UPDATE servicerequest.service_request_completions SET \"RejectReasonCode\" = 99 " +
                "WHERE \"Status\" = 3 AND \"RejectReasonCode\" IS NULL;");        // CompletionStatus.RejectedByOwner
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CancelReasonCode",
                schema: "servicerequest",
                table: "service_requests");

            migrationBuilder.DropColumn(
                name: "RejectReasonCode",
                schema: "servicerequest",
                table: "service_request_offers");

            migrationBuilder.DropColumn(
                name: "RejectReasonCode",
                schema: "servicerequest",
                table: "service_request_completions");

            migrationBuilder.DropColumn(
                name: "RejectReasonCode",
                schema: "servicerequest",
                table: "service_request_assignments");
        }
    }
}
