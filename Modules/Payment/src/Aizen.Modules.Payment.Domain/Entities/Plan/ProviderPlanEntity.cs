using Aizen.Core.Domain;
using Aizen.Modules.Payment.Abstraction;

namespace Aizen.Modules.Payment.Domain.Entities.Plan;

[DocumentationInfo("Provider plan entity",
    "Admin-managed provider subscription plan. Commission rates are stored in CommissionRuleEntity (Plan type).")]
public sealed class ProviderPlanEntity : AizenEntityWithAudit
{
    public string  PlanCode           { get; private set; } = default!;  // FREE / STANDARD / PREMIUM_PARTNER
    public string  Name               { get; private set; } = default!;
    public string? Description        { get; private set; }
    public decimal MonthlyPriceTRY    { get; private set; }
    public int?    MaxActiveOffers    { get; private set; }  // null = unlimited
    public bool    HasPriorityBoost   { get; private set; }
    public bool    HasFullAnalytics   { get; private set; }
    public int     SortOrder          { get; private set; }
    public DateTime? ValidFrom        { get; private set; }
    public DateTime? ValidTo          { get; private set; }

    private ProviderPlanEntity() { }

    public static ProviderPlanEntity Create(
        string planCode, string name, string? description,
        decimal monthlyPriceTRY, int? maxActiveOffers,
        bool hasPriorityBoost, bool hasFullAnalytics, int sortOrder)
    {
        return new ProviderPlanEntity
        {
            PlanCode         = planCode.ToUpperInvariant(),
            Name             = name,
            Description      = description,
            MonthlyPriceTRY  = monthlyPriceTRY,
            MaxActiveOffers  = maxActiveOffers,
            HasPriorityBoost = hasPriorityBoost,
            HasFullAnalytics = hasFullAnalytics,
            SortOrder        = sortOrder,
            IsActive         = true,
        };
    }

    public void Update(string name, string? description, decimal monthlyPriceTRY,
        int? maxActiveOffers, bool hasPriorityBoost, bool hasFullAnalytics, int sortOrder)
    {
        Name             = name;
        Description      = description;
        MonthlyPriceTRY  = monthlyPriceTRY;
        MaxActiveOffers  = maxActiveOffers;
        HasPriorityBoost = hasPriorityBoost;
        HasFullAnalytics = hasFullAnalytics;
        SortOrder        = sortOrder;
    }

    public bool IsFree => MonthlyPriceTRY == 0;
}
