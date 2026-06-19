using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aizen.Modules.Vessel.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddVesselUiContractFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AssetType",
                schema: "vessel",
                table: "vessels",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OperationalStatus",
                schema: "vessel",
                table: "vessels",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BuildCountry",
                schema: "vessel",
                table: "vessel_specifications",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CrewCapacity",
                schema: "vessel",
                table: "vessel_specifications",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "GrossTonnage",
                schema: "vessel",
                table: "vessel_specifications",
                type: "numeric(14,2)",
                precision: 14,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "NetTonnage",
                schema: "vessel",
                table: "vessel_specifications",
                type: "numeric(14,2)",
                precision: 14,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PassengerCapacity",
                schema: "vessel",
                table: "vessel_specifications",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SuperstructureMaterial",
                schema: "vessel",
                table: "vessel_specifications",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                schema: "vessel",
                table: "vessel_media",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ThumbnailUrl",
                schema: "vessel",
                table: "vessel_media",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                schema: "vessel",
                table: "vessel_media",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "UploadedByUserId",
                schema: "vessel",
                table: "vessel_media",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CruisingSpeedKnots",
                schema: "vessel",
                table: "vessel_engines",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "EnginePowerKw",
                schema: "vessel",
                table: "vessel_engines",
                type: "numeric(14,2)",
                precision: 14,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FuelCapacityL",
                schema: "vessel",
                table: "vessel_engines",
                type: "numeric(14,2)",
                precision: 14,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxSpeedKnots",
                schema: "vessel",
                table: "vessel_engines",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PropulsionType",
                schema: "vessel",
                table: "vessel_engines",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RangeNm",
                schema: "vessel",
                table: "vessel_engines",
                type: "numeric(14,2)",
                precision: 14,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                schema: "vessel",
                table: "vessel_documents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ApprovedByUserId",
                schema: "vessel",
                table: "vessel_documents",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DocumentCategory",
                schema: "vessel",
                table: "vessel_documents",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IssuingAuthority",
                schema: "vessel",
                table: "vessel_documents",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_vessels_AssetType",
                schema: "vessel",
                table: "vessels",
                column: "AssetType");

            migrationBuilder.CreateIndex(
                name: "IX_vessels_OperationalStatus",
                schema: "vessel",
                table: "vessels",
                column: "OperationalStatus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_vessels_AssetType",
                schema: "vessel",
                table: "vessels");

            migrationBuilder.DropIndex(
                name: "IX_vessels_OperationalStatus",
                schema: "vessel",
                table: "vessels");

            migrationBuilder.DropColumn(
                name: "AssetType",
                schema: "vessel",
                table: "vessels");

            migrationBuilder.DropColumn(
                name: "OperationalStatus",
                schema: "vessel",
                table: "vessels");

            migrationBuilder.DropColumn(
                name: "BuildCountry",
                schema: "vessel",
                table: "vessel_specifications");

            migrationBuilder.DropColumn(
                name: "CrewCapacity",
                schema: "vessel",
                table: "vessel_specifications");

            migrationBuilder.DropColumn(
                name: "GrossTonnage",
                schema: "vessel",
                table: "vessel_specifications");

            migrationBuilder.DropColumn(
                name: "NetTonnage",
                schema: "vessel",
                table: "vessel_specifications");

            migrationBuilder.DropColumn(
                name: "PassengerCapacity",
                schema: "vessel",
                table: "vessel_specifications");

            migrationBuilder.DropColumn(
                name: "SuperstructureMaterial",
                schema: "vessel",
                table: "vessel_specifications");

            migrationBuilder.DropColumn(
                name: "Description",
                schema: "vessel",
                table: "vessel_media");

            migrationBuilder.DropColumn(
                name: "ThumbnailUrl",
                schema: "vessel",
                table: "vessel_media");

            migrationBuilder.DropColumn(
                name: "Title",
                schema: "vessel",
                table: "vessel_media");

            migrationBuilder.DropColumn(
                name: "UploadedByUserId",
                schema: "vessel",
                table: "vessel_media");

            migrationBuilder.DropColumn(
                name: "CruisingSpeedKnots",
                schema: "vessel",
                table: "vessel_engines");

            migrationBuilder.DropColumn(
                name: "EnginePowerKw",
                schema: "vessel",
                table: "vessel_engines");

            migrationBuilder.DropColumn(
                name: "FuelCapacityL",
                schema: "vessel",
                table: "vessel_engines");

            migrationBuilder.DropColumn(
                name: "MaxSpeedKnots",
                schema: "vessel",
                table: "vessel_engines");

            migrationBuilder.DropColumn(
                name: "PropulsionType",
                schema: "vessel",
                table: "vessel_engines");

            migrationBuilder.DropColumn(
                name: "RangeNm",
                schema: "vessel",
                table: "vessel_engines");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                schema: "vessel",
                table: "vessel_documents");

            migrationBuilder.DropColumn(
                name: "ApprovedByUserId",
                schema: "vessel",
                table: "vessel_documents");

            migrationBuilder.DropColumn(
                name: "DocumentCategory",
                schema: "vessel",
                table: "vessel_documents");

            migrationBuilder.DropColumn(
                name: "IssuingAuthority",
                schema: "vessel",
                table: "vessel_documents");
        }
    }
}
