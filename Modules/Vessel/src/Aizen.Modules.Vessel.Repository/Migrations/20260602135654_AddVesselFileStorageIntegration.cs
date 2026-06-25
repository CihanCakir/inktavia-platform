using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aizen.Modules.Vessel.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddVesselFileStorageIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_vessel_media_VesselId_MediaType",
                schema: "vessel",
                table: "vessel_media");

            migrationBuilder.DropColumn(
                name: "FileUrl",
                schema: "vessel",
                table: "vessel_media");

            migrationBuilder.DropColumn(
                name: "FileUrl",
                schema: "vessel",
                table: "vessel_documents");

            migrationBuilder.RenameColumn(
                name: "FileName",
                schema: "vessel",
                table: "vessel_media",
                newName: "OriginalFileNameSnapshot");

            migrationBuilder.RenameColumn(
                name: "MimeType",
                schema: "vessel",
                table: "vessel_documents",
                newName: "ContentTypeSnapshot");

            migrationBuilder.RenameColumn(
                name: "FileName",
                schema: "vessel",
                table: "vessel_documents",
                newName: "OriginalFileNameSnapshot");

            migrationBuilder.Sql(
                @"ALTER TABLE vessel.vessel_media ALTER COLUMN ""FileId"" TYPE uuid USING ""FileId""::uuid");

            migrationBuilder.AddColumn<string>(
                name: "ContentTypeSnapshot",
                schema: "vessel",
                table: "vessel_media",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SizeInBytesSnapshot",
                schema: "vessel",
                table: "vessel_media",
                type: "bigint",
                nullable: true);

            migrationBuilder.Sql(
                @"ALTER TABLE vessel.vessel_documents ALTER COLUMN ""FileId"" TYPE uuid USING ""FileId""::uuid");

            migrationBuilder.AddColumn<long>(
                name: "SizeInBytesSnapshot",
                schema: "vessel",
                table: "vessel_documents",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_vessel_media_VesselId_SortOrder",
                schema: "vessel",
                table: "vessel_media",
                columns: new[] { "VesselId", "SortOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_vessel_media_VesselId_SortOrder",
                schema: "vessel",
                table: "vessel_media");

            migrationBuilder.DropColumn(
                name: "ContentTypeSnapshot",
                schema: "vessel",
                table: "vessel_media");

            migrationBuilder.DropColumn(
                name: "SizeInBytesSnapshot",
                schema: "vessel",
                table: "vessel_media");

            migrationBuilder.DropColumn(
                name: "SizeInBytesSnapshot",
                schema: "vessel",
                table: "vessel_documents");

            migrationBuilder.RenameColumn(
                name: "OriginalFileNameSnapshot",
                schema: "vessel",
                table: "vessel_media",
                newName: "FileName");

            migrationBuilder.RenameColumn(
                name: "OriginalFileNameSnapshot",
                schema: "vessel",
                table: "vessel_documents",
                newName: "FileName");

            migrationBuilder.RenameColumn(
                name: "ContentTypeSnapshot",
                schema: "vessel",
                table: "vessel_documents",
                newName: "MimeType");

            migrationBuilder.AlterColumn<string>(
                name: "FileId",
                schema: "vessel",
                table: "vessel_media",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FileUrl",
                schema: "vessel",
                table: "vessel_media",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FileId",
                schema: "vessel",
                table: "vessel_documents",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FileUrl",
                schema: "vessel",
                table: "vessel_documents",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_vessel_media_VesselId_MediaType",
                schema: "vessel",
                table: "vessel_media",
                columns: new[] { "VesselId", "MediaType" });
        }
    }
}
