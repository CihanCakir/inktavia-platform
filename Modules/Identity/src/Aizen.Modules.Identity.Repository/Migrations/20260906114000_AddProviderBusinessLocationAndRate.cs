using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aizen.Modules.Identity.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddProviderBusinessLocationAndRate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BusinessAddressLabel",
                table: "UserProfiles",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BusinessLatitude",
                table: "UserProfiles",
                type: "numeric(10,7)",
                precision: 10,
                scale: 7,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BusinessLongitude",
                table: "UserProfiles",
                type: "numeric(10,7)",
                precision: 10,
                scale: 7,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RatePerKm",
                table: "UserProfiles",
                type: "numeric(12,4)",
                precision: 12,
                scale: 4,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BusinessAddressLabel",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "BusinessLatitude",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "BusinessLongitude",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "RatePerKm",
                table: "UserProfiles");
        }
    }
}
