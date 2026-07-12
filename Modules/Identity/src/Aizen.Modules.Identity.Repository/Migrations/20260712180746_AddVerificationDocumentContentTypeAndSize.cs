using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aizen.Modules.Identity.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddVerificationDocumentContentTypeAndSize : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContentType",
                table: "VerificationDocuments",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SizeInBytes",
                table: "VerificationDocuments",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            // Backfill ContentType from legacy Format column for existing rows
            migrationBuilder.Sql(
                @"UPDATE ""VerificationDocuments"" SET ""ContentType"" = ""Format"" WHERE ""ContentType"" IS NULL AND ""Format"" IS NOT NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContentType",
                table: "VerificationDocuments");

            migrationBuilder.DropColumn(
                name: "SizeInBytes",
                table: "VerificationDocuments");
        }
    }
}
