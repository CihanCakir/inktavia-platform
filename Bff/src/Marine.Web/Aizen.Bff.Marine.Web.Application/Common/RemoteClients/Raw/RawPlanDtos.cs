namespace Aizen.Bff.Marine.Web.Application.Common.RemoteClients.Raw;

/// <summary>
/// BFF-local mirrors of the Payment module's public plan JSON (the <c>[AllowAnonymous]</c> GETs on
/// <c>provider-plans</c> / <c>participant-plans</c>). We deliberately do NOT reference the module types: the provider
/// DTO lives in <c>Payment.Abstraction</c> but the participant DTO lives in <c>Payment.Application</c> with a Domain
/// dependency, so binding the wire shape here keeps the BFF on the <c>.Abstraction</c>-only boundary and symmetric
/// across both plan kinds. Only the published, web-safe fields are mirrored; internal fields (database <c>Id</c>,
/// per-viewer <c>IsCurrent</c>) are simply not declared, so Refit ignores them.
/// </summary>
public sealed class RawProviderPlanDto
{
    public string PlanCode { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public decimal MonthlyPriceTRY { get; set; }
    public decimal? AnnualPriceTRY { get; set; }
    public string? BadgeLabel { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public List<string> Features { get; set; } = new();
    public RawPlanActivePriceDto? ActivePrice { get; set; }
}

public sealed class RawParticipantPlanDto
{
    public string PlanCode { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public decimal MonthlyPriceTRY { get; set; }
    public decimal? AnnualPriceTRY { get; set; }
    public int? TrialDays { get; set; }
    public string? BadgeLabel { get; set; }
    public bool IsFree { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
    public DateTime? ValidFrom { get; set; }
    public List<RawPlanFeatureItemDto> FeatureItems { get; set; } = new();
}

/// <summary>A single authored pricing-card bullet.</summary>
public sealed class RawPlanFeatureItemDto
{
    public string Text { get; set; } = default!;
    public bool IsHighlighted { get; set; }
}

/// <summary>The versioned plan price active now (provider plans) — only the effective-from instant is surfaced.</summary>
public sealed class RawPlanActivePriceDto
{
    public DateTime EffectiveFrom { get; set; }
}
