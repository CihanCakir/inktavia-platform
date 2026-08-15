using Aizen.Bff.Marine.Web.Application.Common.RemoteClients.Raw;
using Aizen.Bff.Marine.Web.Application.Contracts.Pricing;

namespace Aizen.Bff.Marine.Web.Application.Pricing;

/// <summary>
/// Payment public plan JSON (raw mirrors) → web pricing DTOs. Strips database ids and per-viewer/economics fields;
/// flattens authored bullets to a uniform <c>Features</c> list. Field-stripping proven in W5.
/// </summary>
public static class WebPricingMapper
{
    private const string ProviderAudience = "provider";
    private const string ParticipantAudience = "participant";

    public static WebPlanDto ToWebPlan(RawProviderPlanDto p) => new()
    {
        Audience = ProviderAudience,
        PlanCode = p.PlanCode,
        Name = p.Name,
        Description = p.Description,
        MonthlyPriceTRY = p.MonthlyPriceTRY,
        AnnualPriceTRY = p.AnnualPriceTRY,
        BadgeLabel = p.BadgeLabel,
        TrialDays = null,
        Features = new List<string>(p.Features),
        SortOrder = p.SortOrder,
        EffectiveFrom = ToUtcOffset(p.ActivePrice?.EffectiveFrom),
    };

    public static WebPlanDto ToWebPlan(RawParticipantPlanDto p) => new()
    {
        Audience = ParticipantAudience,
        PlanCode = p.PlanCode,
        Name = p.Name,
        Description = p.Description,
        MonthlyPriceTRY = p.MonthlyPriceTRY,
        AnnualPriceTRY = p.AnnualPriceTRY,
        BadgeLabel = p.BadgeLabel,
        TrialDays = p.TrialDays,
        Features = p.FeatureItems.Select(f => f.Text).ToList(),
        SortOrder = p.SortOrder,
        EffectiveFrom = ToUtcOffset(p.ValidFrom),
    };

    // Wire DateTimes arrive with an unpredictable Kind; normalize to a UTC-based offset so the projection never throws.
    private static DateTimeOffset? ToUtcOffset(DateTime? value)
        => value is { } v ? new DateTimeOffset(DateTime.SpecifyKind(v, DateTimeKind.Utc)) : null;
}
