using Aizen.Modules.Identity.Domain.Entities.ProviderServiceCategory;
using FluentAssertions;

namespace Aizen.Modules.Identity.Domain.UnitTests.Entities;

/// <summary>
/// CANON — the onboarding→canonical map shared by the live onboarding write path, the backfill seeder, and the data
/// migration. Proves each legacy id resolves to the canonical stored form <c>lower(SERVICE_PROVIDER_CATEGORY.Code)</c>,
/// the map is idempotent (canonical passes through unchanged), and the electrical/electronics merge collapses to one
/// target.
/// </summary>
public sealed class ProviderServiceCategoryCanonicalMapTests
{
    [Theory]
    [InlineData("engine-mechanical", "motor_maintenance")]
    [InlineData("hull-paint",        "hull_maintenance")]
    [InlineData("electrical",        "electrical_service")]
    [InlineData("electronics",       "electrical_service")]  // merge → shared target
    [InlineData("rigging-sails",     "rigging_sails")]
    [InlineData("cleaning-care",     "boat_cleaning")]
    [InlineData("upholstery",        "upholstery")]          // legacy id already equals lower(Code)
    [InlineData("concierge-support", "concierge_support")]
    public void Maps_each_legacy_onboarding_id_to_canonical(string legacy, string canonical)
        => ProviderServiceCategoryCanonicalMap.ToCanonical(legacy).Should().Be(canonical);

    [Theory]
    [InlineData("ENGINE-MECHANICAL", "motor_maintenance")]
    [InlineData("  hull-paint  ",    "hull_maintenance")]
    public void Normalizes_case_and_whitespace_before_mapping(string messy, string canonical)
        => ProviderServiceCategoryCanonicalMap.ToCanonical(messy).Should().Be(canonical);

    [Fact]
    public void Is_idempotent_canonical_values_pass_through_unchanged()
    {
        foreach (var canonical in new[]
                 {
                     "motor_maintenance", "hull_maintenance", "electrical_service", "rigging_sails",
                     "boat_cleaning", "upholstery", "concierge_support",
                 })
        {
            ProviderServiceCategoryCanonicalMap.ToCanonical(canonical).Should().Be(canonical);
            // Applying twice equals applying once.
            ProviderServiceCategoryCanonicalMap.ToCanonical(ProviderServiceCategoryCanonicalMap.ToCanonical(canonical))
                .Should().Be(canonical);
        }
    }

    [Fact]
    public void Electrical_and_electronics_collapse_to_one_canonical_target()
        => ProviderServiceCategoryCanonicalMap.ToCanonical("electrical")
            .Should().Be(ProviderServiceCategoryCanonicalMap.ToCanonical("electronics"));

    [Fact]
    public void Unknown_value_passes_through_lowercased()
        => ProviderServiceCategoryCanonicalMap.ToCanonical("Some-Unknown-Cat").Should().Be("some-unknown-cat");
}
