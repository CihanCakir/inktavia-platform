using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aizen.Modules.ServiceRequest.Repository.Migrations
{
    /// <inheritdoc />
    public partial class RemoveVesselSnapshotColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VesselLengthUnitCode",
                schema: "servicerequest",
                table: "service_requests");

            migrationBuilder.DropColumn(
                name: "VesselLengthValue",
                schema: "servicerequest",
                table: "service_requests");

            migrationBuilder.DropColumn(
                name: "VesselManufacturer",
                schema: "servicerequest",
                table: "service_requests");

            migrationBuilder.DropColumn(
                name: "VesselModel",
                schema: "servicerequest",
                table: "service_requests");

            migrationBuilder.DropColumn(
                name: "VesselTypeCode",
                schema: "servicerequest",
                table: "service_requests");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VesselLengthUnitCode",
                schema: "servicerequest",
                table: "service_requests",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "VesselLengthValue",
                schema: "servicerequest",
                table: "service_requests",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VesselManufacturer",
                schema: "servicerequest",
                table: "service_requests",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VesselModel",
                schema: "servicerequest",
                table: "service_requests",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VesselTypeCode",
                schema: "servicerequest",
                table: "service_requests",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }
    }
}
