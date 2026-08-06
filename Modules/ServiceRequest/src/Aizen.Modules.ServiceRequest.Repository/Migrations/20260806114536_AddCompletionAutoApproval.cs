using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aizen.Modules.ServiceRequest.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddCompletionAutoApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AutoApproveAt",
                schema: "servicerequest",
                table: "service_request_completions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AutoApproveReminderSentAt",
                schema: "servicerequest",
                table: "service_request_completions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_service_request_completions_AutoApproveAt",
                schema: "servicerequest",
                table: "service_request_completions",
                column: "AutoApproveAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_service_request_completions_AutoApproveAt",
                schema: "servicerequest",
                table: "service_request_completions");

            migrationBuilder.DropColumn(
                name: "AutoApproveAt",
                schema: "servicerequest",
                table: "service_request_completions");

            migrationBuilder.DropColumn(
                name: "AutoApproveReminderSentAt",
                schema: "servicerequest",
                table: "service_request_completions");
        }
    }
}
