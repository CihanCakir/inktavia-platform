using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aizen.Modules.Payment.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddProviderSubMerchantOnboardingStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OnboardingStatus",
                schema: "payment",
                table: "provider_payment_profiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // ── BE-I1 backfill (idempotent; enum: NotStarted=0, SubMerchantCreated=2, Verified=3, Suspended=5, Blocked=6) ──
            // 1) Key-based baseline: verified key → Verified(3); key but not verified → SubMerchantCreated(2); no key → NotStarted(0).
            migrationBuilder.Sql(@"
                UPDATE payment.provider_payment_profiles
                SET ""OnboardingStatus"" = CASE
                    WHEN ""SubMerchantKey"" IS NOT NULL AND ""SubMerchantKey"" <> '' AND ""VerifiedAt"" IS NOT NULL THEN 3
                    WHEN ""SubMerchantKey"" IS NOT NULL AND ""SubMerchantKey"" <> ''                              THEN 2
                    ELSE 0
                END;");
            // 2) Legacy string Status overrides win: OnHold → Suspended(5), Blocked → Blocked(6).
            migrationBuilder.Sql(@"UPDATE payment.provider_payment_profiles SET ""OnboardingStatus"" = 5 WHERE ""Status"" = 'OnHold';");
            migrationBuilder.Sql(@"UPDATE payment.provider_payment_profiles SET ""OnboardingStatus"" = 6 WHERE ""Status"" = 'Blocked';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OnboardingStatus",
                schema: "payment",
                table: "provider_payment_profiles");
        }
    }
}
