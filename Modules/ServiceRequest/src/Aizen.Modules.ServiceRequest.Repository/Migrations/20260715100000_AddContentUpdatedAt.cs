using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aizen.Modules.ServiceRequest.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddContentUpdatedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ContentUpdatedAt",
                schema: "servicerequest",
                table: "service_requests",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContentUpdatedAt",
                schema: "servicerequest",
                table: "service_requests");
        }
    }
}
