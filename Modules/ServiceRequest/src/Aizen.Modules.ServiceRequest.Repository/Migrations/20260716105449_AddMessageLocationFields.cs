using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aizen.Modules.ServiceRequest.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddMessageLocationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LocationLabel",
                schema: "servicerequest",
                table: "service_request_messages",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LocationLat",
                schema: "servicerequest",
                table: "service_request_messages",
                type: "numeric(10,7)",
                precision: 10,
                scale: 7,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LocationLng",
                schema: "servicerequest",
                table: "service_request_messages",
                type: "numeric(10,7)",
                precision: 10,
                scale: 7,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LocationLabel",
                schema: "servicerequest",
                table: "service_request_messages");

            migrationBuilder.DropColumn(
                name: "LocationLat",
                schema: "servicerequest",
                table: "service_request_messages");

            migrationBuilder.DropColumn(
                name: "LocationLng",
                schema: "servicerequest",
                table: "service_request_messages");
        }
    }
}
