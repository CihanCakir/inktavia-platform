using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aizen.Modules.ServiceRequest.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddCargoDrySupplyProductCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CargoDryProductCode",
                schema: "servicerequest",
                table: "service_requests",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_service_requests_Vessel_CargoDryProduct_Status",
                schema: "servicerequest",
                table: "service_requests",
                columns: new[] { "VesselId", "CargoDryProductCode", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_service_requests_Vessel_CargoDryProduct_Status",
                schema: "servicerequest",
                table: "service_requests");

            migrationBuilder.DropColumn(
                name: "CargoDryProductCode",
                schema: "servicerequest",
                table: "service_requests");
        }
    }
}
