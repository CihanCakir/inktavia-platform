namespace Aizen.Modules.Identity.Domain.Entities.ProviderServiceCategory;

/// <summary>
/// CANON — maps a legacy onboarding service-category id to the canonical stored form
/// <c>lower(SERVICE_PROVIDER_CATEGORY.Code)</c> (underscores preserved, e.g. <c>motor_maintenance</c>). This is the
/// single source of truth shared by the live onboarding write path and the backfill seeder, and it mirrors the data
/// migration that remaps existing rows. Storing the canonical form aligns the provider-eligibility read-model with
/// the ServiceRequest category vocabulary (which already uses SERVICE_PROVIDER_CATEGORY codes), so the N-C region
/// fan-out and the M2 availability filter match on one shared vocabulary.
/// </summary>
public static class ProviderServiceCategoryCanonicalMap
{
    /// <summary>
    /// Legacy onboarding id → canonical stored form. NOTE: <c>electrical</c> and <c>electronics</c> both map to
    /// <c>electrical_service</c> (an approved merge) — this is lossy on reverse (the canonical value cannot be split
    /// back to the two sources).
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> LegacyToCanonical =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["engine-mechanical"] = "motor_maintenance",
            ["hull-paint"]        = "hull_maintenance",
            ["electrical"]        = "electrical_service",
            ["electronics"]       = "electrical_service",   // merge → shared target (lossy on reverse)
            ["rigging-sails"]     = "rigging_sails",
            ["cleaning-care"]     = "boat_cleaning",
            ["upholstery"]        = "upholstery",
            ["concierge-support"] = "concierge_support",
        };

    /// <summary>
    /// Returns the canonical stored form for a raw category value. A known legacy onboarding id is remapped; any
    /// other value (an already-canonical code sent by the post-CANON-e frontend, or an unknown value) passes through
    /// normalized to <c>lower</c>. This makes the write path safe to deploy BEFORE the frontend switches — it accepts
    /// both the legacy ids and the canonical codes during the transition.
    /// </summary>
    public static string ToCanonical(string rawCategory)
    {
        var normalized = rawCategory.Trim().ToLowerInvariant();
        return LegacyToCanonical.TryGetValue(normalized, out var canonical) ? canonical : normalized;
    }
}
