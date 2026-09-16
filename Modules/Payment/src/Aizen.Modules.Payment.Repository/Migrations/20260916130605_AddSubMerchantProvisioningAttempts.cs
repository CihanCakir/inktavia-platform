using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aizen.Modules.Payment.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddSubMerchantProvisioningAttempts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AttemptCount",
                schema: "payment",
                table: "provider_payment_profiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "KycPayloadEncrypted",
                schema: "payment",
                table: "provider_payment_profiles",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastAttemptAtUtc",
                schema: "payment",
                table: "provider_payment_profiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastAttemptError",
                schema: "payment",
                table: "provider_payment_profiles",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_provider_payment_profiles_OnboardingStatus_LastAttemptAtUtc",
                schema: "payment",
                table: "provider_payment_profiles",
                columns: new[] { "OnboardingStatus", "LastAttemptAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_provider_payment_profiles_OnboardingStatus_LastAttemptAtUtc",
                schema: "payment",
                table: "provider_payment_profiles");

            migrationBuilder.DropColumn(
                name: "AttemptCount",
                schema: "payment",
                table: "provider_payment_profiles");

            migrationBuilder.DropColumn(
                name: "KycPayloadEncrypted",
                schema: "payment",
                table: "provider_payment_profiles");

            migrationBuilder.DropColumn(
                name: "LastAttemptAtUtc",
                schema: "payment",
                table: "provider_payment_profiles");

            migrationBuilder.DropColumn(
                name: "LastAttemptError",
                schema: "payment",
                table: "provider_payment_profiles");
        }
    }
}
