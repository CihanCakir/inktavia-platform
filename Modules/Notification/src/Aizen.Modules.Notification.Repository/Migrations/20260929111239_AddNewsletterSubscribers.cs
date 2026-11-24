using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Aizen.Modules.Notification.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddNewsletterSubscribers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "newsletter_subscribers",
                schema: "notification",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    Lang = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    SourcePage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ConfirmationToken = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ConfirmationTokenExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConfirmedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UnsubscribedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UnsubscribeToken = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SpamScore = table.Column<int>(type: "integer", nullable: false),
                    IpHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_newsletter_subscribers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_newsletter_subscribers_ConfirmationToken",
                schema: "notification",
                table: "newsletter_subscribers",
                column: "ConfirmationToken");

            migrationBuilder.CreateIndex(
                name: "IX_newsletter_subscribers_Email",
                schema: "notification",
                table: "newsletter_subscribers",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_newsletter_subscribers_IpHash_CreatedAt",
                schema: "notification",
                table: "newsletter_subscribers",
                columns: new[] { "IpHash", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_newsletter_subscribers_Status_CreatedAt",
                schema: "notification",
                table: "newsletter_subscribers",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_newsletter_subscribers_UnsubscribeToken",
                schema: "notification",
                table: "newsletter_subscribers",
                column: "UnsubscribeToken",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "newsletter_subscribers",
                schema: "notification");
        }
    }
}
