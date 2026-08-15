namespace Aizen.Bff.Marine.Web.Application.Contracts.Pricing;

/// <summary>
/// Public pricing for the website (W4 priority 5, PARTIAL). Carries ONLY published subscription-plan terms — the
/// commercially public part of pricing that a module actually exposes anonymously. Commission model, customer
/// platform fee and the VAT flag are admin-only in Payment and are OMITTED (reported BLOCKED), never faked.
/// </summary>
public sealed class WebPricingDto
{
    /// <summary>Settlement currency for every amount below. The marketplace settles in TRY.</summary>
    public string Currency { get; set; } = "TRY";

    public List<WebPlanDto> ProviderPlans { get; set; } = new();
    public List<WebPlanDto> ParticipantPlans { get; set; } = new();
}

/// <summary>
/// One subscription tier for a pricing card. A web-facing reshape of the module plan DTOs: the stable
/// <see cref="PlanCode"/> is the public key (database <c>Id</c> omitted), authored bullets are flattened to
/// <see cref="Features"/>, and per-viewer / internal flags (<c>IsCurrent</c>, discount-rate mechanics) are not
/// surfaced. Uniform across provider and participant plans.
/// </summary>
public sealed class WebPlanDto
{
    /// <summary><c>provider</c> or <c>participant</c> — which audience this tier is for.</summary>
    public string Audience { get; set; } = default!;

    public string PlanCode { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }

    public decimal MonthlyPriceTRY { get; set; }
    public decimal? AnnualPriceTRY { get; set; }

    public string? BadgeLabel { get; set; }

    /// <summary>Free-trial length in days, where the tier offers one (participant tiers); null otherwise.</summary>
    public int? TrialDays { get; set; }

    /// <summary>Authored pricing-card bullets (already public marketing copy).</summary>
    public List<string> Features { get; set; } = new();

    public int SortOrder { get; set; }

    /// <summary>When this tier's current price took effect (provider: active price; participant: valid-from).</summary>
    public DateTimeOffset? EffectiveFrom { get; set; }
}
