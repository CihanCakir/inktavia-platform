using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aizen.Modules.ServiceRequest.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddDiscoveryColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PublishedAt",
                schema: "servicerequest",
                table: "service_requests",
                type: "timestamp with time zone",
                nullable: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_service_requests_Status_LocationCityCode",
                schema: "servicerequest",
                table: "service_requests",
                columns: new[] { "Status", "LocationCityCode" });

            migrationBuilder.CreateIndex(
                name: "IX_service_requests_Status_PublishedAt",
                schema: "servicerequest",
                table: "service_requests",
                columns: new[] { "Status", "PublishedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_service_requests_Status_LocationCityCode",
                schema: "servicerequest",
                table: "service_requests");

            migrationBuilder.DropIndex(
                name: "IX_service_requests_Status_PublishedAt",
                schema: "servicerequest",
                table: "service_requests");

            migrationBuilder.DropColumn(
                name: "PublishedAt",
                schema: "servicerequest",
                table: "service_requests");

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
    }
}
