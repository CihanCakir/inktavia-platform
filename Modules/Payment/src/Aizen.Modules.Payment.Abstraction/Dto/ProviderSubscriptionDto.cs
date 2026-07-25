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
}
