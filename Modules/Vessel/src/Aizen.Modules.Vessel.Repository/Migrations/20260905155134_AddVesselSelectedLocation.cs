using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aizen.Modules.Vessel.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddVesselSelectedLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SelectedLocationCustomLabel",
                schema: "vessel",
                table: "vessels",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SelectedLocationLatitude",
                schema: "vessel",
                table: "vessels",
                type: "numeric(10,7)",
                precision: 10,
                scale: 7,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SelectedLocationLongitude",
                schema: "vessel",
                table: "vessels",
                type: "numeric(10,7)",
                precision: 10,
                scale: 7,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SelectedLocationMarinaId",
                schema: "vessel",
                table: "vessels",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SelectedLocationMarinaName",
                schema: "vessel",
                table: "vessels",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SelectedLocationSetAt",
                schema: "vessel",
                table: "vessels",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SelectedLocationCustomLabel",
                schema: "vessel",
                table: "vessels");

            migrationBuilder.DropColumn(
                name: "SelectedLocationLatitude",
                schema: "vessel",
                table: "vessels");

            migrationBuilder.DropColumn(
                name: "SelectedLocationLongitude",
                schema: "vessel",
                table: "vessels");

            migrationBuilder.DropColumn(
                name: "SelectedLocationMarinaId",
                schema: "vessel",
                table: "vessels");

            migrationBuilder.DropColumn(
                name: "SelectedLocationMarinaName",
                schema: "vessel",
                table: "vessels");

            migrationBuilder.DropColumn(
                name: "SelectedLocationSetAt",
                schema: "vessel",
                table: "vessels");
        }
    }
}
