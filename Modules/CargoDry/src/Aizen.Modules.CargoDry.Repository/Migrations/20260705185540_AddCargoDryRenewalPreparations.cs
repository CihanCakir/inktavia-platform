using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Aizen.Modules.CargoDry.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddCargoDryRenewalPreparations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "renewal_preparations",
                schema: "cargodry",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RenewalCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    KitId = table.Column<long>(type: "bigint", nullable: false),
                    KitCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ProductCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OwnerUserId = table.Column<long>(type: "bigint", nullable: true),
                    VesselId = table.Column<long>(type: "bigint", nullable: true),
                    ProviderProfileId = table.Column<long>(type: "bigint", nullable: true),
                    CurrentExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RequestedRenewalMonths = table.Column<int>(type: "integer", nullable: false),
                    NewExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RenewalPrice = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    InvoiceId = table.Column<long>(type: "bigint", nullable: true),
                    PaymentTransactionId = table.Column<long>(type: "bigint", nullable: true),
                    ManualPaymentReference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    NotificationCorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    NotificationStatus = table.Column<int>(type: "integer", nullable: false),
                    NotificationChannels = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    LastNotificationTemplateCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LastNotificationLanguageCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    NotificationPreparedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    NotificationPreparedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    NotificationDispatchedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    NotificationDispatchedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    NotificationFailureReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    PreparedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    PreparedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CancelledByUserId = table.Column<long>(type: "bigint", nullable: true),
                    CancelledAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CancellationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifyHost = table.Column<string>(type: "text", nullable: true),
                    ModifyUserId = table.Column<long>(type: "bigint", nullable: true),
                    ModifyDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    CreateHost = table.Column<string>(type: "text", nullable: true),
                    CreateDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_renewal_preparations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_renewal_preparations_KitCode",
                schema: "cargodry",
                table: "renewal_preparations",
                column: "KitCode");

            migrationBuilder.CreateIndex(
                name: "IX_renewal_preparations_KitId",
                schema: "cargodry",
                table: "renewal_preparations",
                column: "KitId");

            migrationBuilder.CreateIndex(
                name: "IX_renewal_preparations_KitId_Status",
                schema: "cargodry",
                table: "renewal_preparations",
                columns: new[] { "KitId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_renewal_preparations_OwnerUserId",
                schema: "cargodry",
                table: "renewal_preparations",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_renewal_preparations_PreparedAtUtc",
                schema: "cargodry",
                table: "renewal_preparations",
                column: "PreparedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_renewal_preparations_ProductCode",
                schema: "cargodry",
                table: "renewal_preparations",
                column: "ProductCode");

            migrationBuilder.CreateIndex(
                name: "IX_renewal_preparations_RenewalCode",
                schema: "cargodry",
                table: "renewal_preparations",
                column: "RenewalCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_renewal_preparations_Status",
                schema: "cargodry",
                table: "renewal_preparations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_renewal_preparations_VesselId",
                schema: "cargodry",
                table: "renewal_preparations",
                column: "VesselId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "renewal_preparations",
                schema: "cargodry");
        }
    }
}
