using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aizen.Modules.ServiceRequest.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddGeoIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_service_requests_Status_Lat_Lng",
                schema: "servicerequest",
                table: "service_requests",
                columns: new[] { "Status", "LocationLatitude", "LocationLongitude" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_service_requests_Status_Lat_Lng",
                schema: "servicerequest",
                table: "service_requests");
        }
    }
}
