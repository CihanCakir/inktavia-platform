using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aizen.Modules.Identity.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddVerificationDocumentFilePublicId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "FilePublicId",
                table: "VerificationDocuments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_VerificationDocuments_FilePublicId",
                table: "VerificationDocuments",
                column: "FilePublicId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VerificationDocuments_FilePublicId",
                table: "VerificationDocuments");

            migrationBuilder.DropColumn(
                name: "FilePublicId",
                table: "VerificationDocuments");
        }
    }
}
