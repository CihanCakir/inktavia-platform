using System.Reflection;
using Aizen.Bff.Marine.Web.Application.Common.RemoteClients.Raw;
using Aizen.Bff.Marine.Web.Application.Contracts.Pricing;
using Aizen.Bff.Marine.Web.Application.Pricing;
using FluentAssertions;

namespace Aizen.Bff.Marine.Web.UnitTests;

/// <summary>
/// W5 — the W4 pricing mapper. Provider and participant plans reshape to one uniform web plan DTO carrying only
/// published marketing terms; the database id and per-viewer/economics fields never surface.
/// </summary>
public sealed class WebPricingMapperTests
{
    private static string[] Props<T>() => typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Select(p => p.Name).ToArray();

    [Fact]
    public void Provider_plan_maps_to_public_terms_with_effective_from()
    {
        var p = new RawProviderPlanDto
        {
            PlanCode = "PRO_GOLD", Name = "Gold", Description = "For busy yards",
            MonthlyPriceTRY = 500, AnnualPriceTRY = 5000, BadgeLabel = "Popular", SortOrder = 2, IsActive = true,
            Features = new() { "Priority boost", "Full analytics" },
            ActivePrice = new RawPlanActivePriceDto { EffectiveFrom = new DateTime(2026, 1, 1) },
        };

        var w = WebPricingMapper.ToWebPlan(p);
        w.Audience.Should().Be("provider");
        w.PlanCode.Should().Be("PRO_GOLD");
        w.MonthlyPriceTRY.Should().Be(500);
        w.AnnualPriceTRY.Should().Be(5000);
        w.Features.Should().Equal("Priority boost", "Full analytics");
        w.TrialDays.Should().BeNull();
        w.EffectiveFrom.Should().Be(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void Participant_plan_maps_trial_and_authored_bullets()
    {
        var p = new RawParticipantPlanDto
        {
            PlanCode = "MEMBER_PLUS", Name = "Plus", MonthlyPriceTRY = 100, TrialDays = 14,
            SortOrder = 1, IsActive = true,
            FeatureItems = new() { new RawPlanFeatureItemDto { Text = "10% service discount" } },
            ValidFrom = new DateTime(2026, 3, 1),
        };

        var w = WebPricingMapper.ToWebPlan(p);
        w.Audience.Should().Be("participant");
        w.TrialDays.Should().Be(14);
        w.Features.Should().Equal("10% service discount");
        w.EffectiveFrom.Should().Be(new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void WebPlanDto_omits_database_id_and_per_viewer_and_economics_fields()
    {
        var names = Props<WebPlanDto>();
        names.Should().NotContain("Id");
        names.Should().NotContain("IsCurrent");
        names.Should().NotContain("IsActive");
        names.Should().NotContain("ServiceDiscountRate", "raw rate mechanics are not surfaced; authored bullets convey benefits");
        names.Should().NotContain("CargoDryDiscountRate");
        names.Should().NotContain("InkCoinEarnMultiplier");
        names.Should().NotContain("MaxActiveOffers");
    }
}
