using Aizen.Modules.ReferenceData.Repository.Mongo;
using FluentAssertions;

namespace Aizen.Modules.ReferenceData.Repository.UnitTests;

/// <summary>
/// M3-1a — the deterministic slug scheme: Turkish-aware slugify, reserved-slug guard, global-unique suffixing, and
/// idempotency (existing slugs are reserved, not reissued).
/// </summary>
public sealed class LocationSlugTests
{
    [Theory]
    [InlineData("İstanbul", "istanbul")]
    [InlineData("Kadıköy", "kadikoy")]
    [InlineData("Şişli", "sisli")]
    [InlineData("Çankaya", "cankaya")]
    [InlineData("Ğüşöçı UPPER", "gusoci-upper")]
    [InlineData("  Multiple   Spaces  ", "multiple-spaces")]
    public void Slugify_is_turkish_aware_and_deterministic(string input, string expected)
    {
        LocationSlugifier.Slugify(input).Should().Be(expected);
        LocationSlugifier.Slugify(input).Should().Be(LocationSlugifier.Slugify(input)); // same input → same output
    }

    [Fact]
    public void Assign_gives_a_numeric_suffix_on_collision()
    {
        var reg = new LocationSlugRegistry();
        reg.Assign("kadikoy").Should().Be("kadikoy");
        reg.Assign("kadikoy").Should().Be("kadikoy-2");
        reg.Assign("kadikoy").Should().Be("kadikoy-3");
    }

    [Fact]
    public void Reserved_slugs_are_suffixed_away()
    {
        var reg = new LocationSlugRegistry();
        // "api"/"new" are reserved (W3.4) → pre-seeded as used, so a base landing on one is suffixed.
        reg.Assign("api").Should().Be("api-2");
        reg.Assign("new").Should().Be("new-2");
    }

    [Fact]
    public void Reserve_makes_assign_idempotent_a_prior_slug_is_not_reissued()
    {
        // Simulate a re-run: the doc already has "kadikoy"; reserving it means a NEW "kadikoy" base gets suffixed.
        var reg = new LocationSlugRegistry();
        reg.Reserve("kadikoy");
        reg.Assign("kadikoy").Should().Be("kadikoy-2", "the existing slug is reserved, not reissued");
    }

    [Fact]
    public void Deterministic_across_runs_same_call_order_same_slugs()
    {
        static List<string> Run()
        {
            var reg = new LocationSlugRegistry();
            // Fixed order (level then code) → fixed output.
            return new List<string>
            {
                reg.Assign("istanbul"),
                reg.Assign("istanbul-kadikoy"),
                reg.Assign("istanbul-kadikoy"),   // collision → -2
                reg.Assign("izmir"),
            };
        }

        Run().Should().Equal("istanbul", "istanbul-kadikoy", "istanbul-kadikoy-2", "izmir");
        Run().Should().Equal(Run(), "same call order yields the same slugs every run");
    }
}
