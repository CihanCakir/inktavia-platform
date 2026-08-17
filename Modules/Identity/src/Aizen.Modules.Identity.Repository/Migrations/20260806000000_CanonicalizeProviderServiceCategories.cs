using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aizen.Modules.Identity.Repository.Migrations
{
    /// <summary>
    /// CANON-d — data-only migration remapping <c>provider_service_categories.ServiceCategoryCode</c> from the legacy
    /// onboarding vocabulary (V1) to the canonical stored form <c>lower(SERVICE_PROVIDER_CATEGORY.Code)</c> (V2). No
    /// schema change. This aligns the provider-eligibility read-model with the ServiceRequest category vocabulary, so
    /// the N-C region fan-out and the M2 availability filter match on one shared vocabulary.
    ///
    /// IDEMPOTENT: each UPDATE matches only a known legacy id in its WHERE clause; already-canonical rows are never
    /// touched, so re-running is a no-op. REVERSIBLE: <see cref="Down"/> restores the legacy ids 1:1 — EXCEPT the
    /// approved merge <c>electrical</c> + <c>electronics</c> → <c>electrical_service</c>, which is LOSSY on reverse
    /// (the two sources cannot be split back; Down restores all such rows to <c>electrical</c>).
    /// <c>upholstery</c> is intentionally absent from both directions: its legacy id already equals lower(Code).
    /// </summary>
    public partial class CanonicalizeProviderServiceCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // legacy id -> canonical lower(Code). WHERE targets the legacy value only => idempotent + skips canonical rows.
            Remap(migrationBuilder, "engine-mechanical", "motor_maintenance");
            Remap(migrationBuilder, "hull-paint",        "hull_maintenance");
            Remap(migrationBuilder, "electrical",        "electrical_service");
            Remap(migrationBuilder, "electronics",       "electrical_service");   // merge -> shared target (lossy on reverse)
            Remap(migrationBuilder, "rigging-sails",     "rigging_sails");
            Remap(migrationBuilder, "cleaning-care",     "boat_cleaning");
            Remap(migrationBuilder, "concierge-support", "concierge_support");
            // "upholstery" -> "upholstery": legacy id already equals lower(Code); no remap needed.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Inverse map (canonical -> legacy). 1:1 rows restore cleanly.
            Remap(migrationBuilder, "motor_maintenance",  "engine-mechanical");
            Remap(migrationBuilder, "hull_maintenance",   "hull-paint");
            Remap(migrationBuilder, "rigging_sails",      "rigging-sails");
            Remap(migrationBuilder, "boat_cleaning",      "cleaning-care");
            Remap(migrationBuilder, "concierge_support",  "concierge-support");

            // LOSSY REVERSE: "electrical" + "electronics" both mapped to "electrical_service"; the two sources cannot
            // be recovered. Down restores every "electrical_service" row to "electrical" (the electronics origin is
            // unrecoverable — accepted per the approved merge).
            Remap(migrationBuilder, "electrical_service", "electrical");
        }

        private static void Remap(MigrationBuilder migrationBuilder, string from, string to)
            => migrationBuilder.Sql(
                $"UPDATE provider_service_categories SET \"ServiceCategoryCode\" = '{to}' " +
                $"WHERE \"ServiceCategoryCode\" = '{from}';");
    }
}
