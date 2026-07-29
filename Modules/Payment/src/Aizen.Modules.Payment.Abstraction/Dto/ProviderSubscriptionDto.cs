namespace Aizen.Modules.Payment.Abstraction.Dto;

public sealed class ProviderSubscriptionDto
{
    public long    SubscriptionId  { get; init; }
    public long    PlanId          { get; init; }
    public string  PlanCode        { get; init; } = default!;
    public string  PlanName        { get; init; } = default!;
    public int     Status          { get; init; }
    public decimal PaidAmount      { get; init; }
    public string  CurrencyCode    { get; init; } = "TRY";
    public decimal MonthlyPriceTRY { get; init; }
    public DateTime PeriodStart    { get; init; }
    public DateTime PeriodEnd      { get; init; }
    public bool    AutoRenew       { get; init; }
    public int     DaysRemaining   { get; init; }
    public bool    IsExpiringSoon  { get; init; }
    public decimal CommissionRateAtSubscription { get; init; }
    public DateTime? CancelledAt   { get; init; }

    // ── BE-P4 launch-vs-list price + upcoming price change (additive; existing fields unchanged) ──
    /// <summary>The versioned <c>ProviderPlanPrice</c> active NOW for this plan (Monthly), or null when none resolves.</summary>
    public ProviderPlanActivePriceDto? ActivePrice { get; init; }
    /// <summary>True when the renewal price (resolved at PeriodEnd) differs from the current PaidAmount (BE-P4).</summary>
    public bool      HasUpcomingPriceChange { get; init; }
    public decimal?  UpcomingPriceAmount    { get; init; }
    public DateTime? UpcomingPriceChangeAt  { get; init; }
}

/// <summary>BE-P4 — the versioned plan price active at an instant: launch-vs-list label + amount + effective window.</summary>
public sealed class ProviderPlanActivePriceDto
{
    /// <summary>"Launch" (go-live campaign window) or "List" (standing price) — reporting label; resolution is date-driven.</summary>
    public string    PriceType     { get; init; } = default!;
    public string    BillingPeriod { get; init; } = "Monthly";
    public decimal   PriceAmount   { get; init; }
    public string    CurrencyCode  { get; init; } = "TRY";
    public DateTime  EffectiveFrom { get; init; }
    public DateTime? EffectiveTo   { get; init; }
    public string?   PriceCode     { get; init; }
}

public sealed class ProviderPlanDto
{
    public long    Id              { get; init; }
    public string  PlanCode        { get; init; } = default!;
    public string  Name            { get; init; } = default!;
    public string? Description     { get; init; }
    public decimal MonthlyPriceTRY { get; init; }
    public decimal? AnnualPriceTRY { get; init; }
    public string? BadgeLabel      { get; init; }
    public int?    MaxActiveOffers { get; init; }
    public bool    HasPriorityBoost{ get; init; }
    public bool    HasFullAnalytics{ get; init; }
    public int     SortOrder       { get; init; }
    public bool    IsCurrent       { get; init; }
    public List<string> Features   { get; init; } = [];

    // ── BE-P4 launch-vs-list price active now (additive; null when no versioned price resolves) ──
    public ProviderPlanActivePriceDto? ActivePrice { get; init; }
}
