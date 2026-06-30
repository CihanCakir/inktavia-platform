using Aizen.Core.Domain;
using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Domain.Entities.Commission;

[DocumentationInfo("Commission rule entity",
    "Data-driven commission rate. Resolution precedence: ProviderOverride > Plan > Category > Global.")]
public sealed class CommissionRuleEntity : AizenEntityWithAudit
{
    public CommissionRuleType RuleType          { get; private set; }
    public string?  CategoryCode               { get; private set; }  // For Category rules
    public long?    ProviderPlanId             { get; private set; }  // For Plan rules
    public long?    ProviderProfileId          { get; private set; }  // For ProviderOverride rules
    public decimal  CommissionRate             { get; private set; }  // e.g. 0.12 = 12%
    public DateTime EffectiveFrom              { get; private set; }
    public DateTime? EffectiveTo               { get; private set; }  // null = never expires
    public string?  Notes                      { get; private set; }

    private CommissionRuleEntity() { }

    public static CommissionRuleEntity CreateGlobal(decimal rate, string? notes = null) =>
        new() { RuleType = CommissionRuleType.Global, CommissionRate = rate, EffectiveFrom = DateTime.UtcNow, Notes = notes, IsActive = true };

    public static CommissionRuleEntity CreateForCategory(string categoryCode, decimal rate, string? notes = null) =>
        new() { RuleType = CommissionRuleType.Category, CategoryCode = categoryCode, CommissionRate = rate, EffectiveFrom = DateTime.UtcNow, Notes = notes, IsActive = true };

    public static CommissionRuleEntity CreateForPlan(long providerPlanId, decimal rate, string? notes = null) =>
        new() { RuleType = CommissionRuleType.Plan, ProviderPlanId = providerPlanId, CommissionRate = rate, EffectiveFrom = DateTime.UtcNow, Notes = notes, IsActive = true };

    public static CommissionRuleEntity CreateProviderOverride(long providerProfileId, decimal rate, DateTime? expiresAt = null, string? notes = null) =>
        new() { RuleType = CommissionRuleType.ProviderOverride, ProviderProfileId = providerProfileId, CommissionRate = rate, EffectiveFrom = DateTime.UtcNow, EffectiveTo = expiresAt, Notes = notes, IsActive = true };

    public void Update(decimal rate, DateTime? expiresAt, string? notes)
    {
        CommissionRate = rate;
        EffectiveTo    = expiresAt;
        Notes          = notes;
    }

    public bool IsEffective(DateTime atUtc) =>
        IsActive && EffectiveFrom <= atUtc && (EffectiveTo == null || EffectiveTo >= atUtc);
}
