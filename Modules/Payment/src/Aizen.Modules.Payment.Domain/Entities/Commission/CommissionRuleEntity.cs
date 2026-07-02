using Aizen.Core.Domain;
using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Domain.Entities.Commission;

[DocumentationInfo("Commission rule entity",
    "Data-driven commission rate. Resolution precedence: ProviderOverride > Plan > Category > Global. " +
    "Extended with RuleCode, Status, Priority, ResolvedAppliedCount for admin panel management. " +
    "Audit fields (CreateUserId, ModifyUserId, CreateDate, ModifyDate) are provided by AizenEntityWithAudit.")]
public sealed class CommissionRuleEntity : AizenEntityWithAudit
{
    // ── Core targeting fields ─────────────────────────────────────────────────
    public CommissionRuleType     RuleType          { get; private set; }
    public string?                CategoryCode      { get; private set; }   // For Category rules
    public long?                  ProviderPlanId    { get; private set; }   // For Plan rules
    public long?                  ProviderProfileId { get; private set; }   // For ProviderOverride rules
    public decimal                CommissionRate    { get; private set; }   // e.g. 0.12 = 12%
    public DateTime               EffectiveFrom     { get; private set; }
    public DateTime?              EffectiveTo       { get; private set; }   // null = never expires
    public string?                Notes             { get; private set; }

    // ── Admin management fields ───────────────────────────────────────────────
    /// <summary>Human-readable code, e.g. "CR-2024-X91". Generated on create.</summary>
    public string?                RuleCode             { get; private set; }
    public CommissionRuleStatus   Status               { get; private set; }
    public CommissionRulePriority Priority             { get; private set; }
    /// <summary>Incremented each time this rule is returned by ResolveRateAsync.</summary>
    public long                   ResolvedAppliedCount { get; private set; }

    private CommissionRuleEntity() { }

    // ── Factory methods ───────────────────────────────────────────────────────

    public static CommissionRuleEntity CreateGlobal(
        decimal rate, DateTime effectiveFrom, DateTime? effectiveTo,
        CommissionRulePriority priority, string? notes, string? ruleCode) =>
        new()
        {
            RuleType       = CommissionRuleType.Global,
            CommissionRate = rate,
            EffectiveFrom  = effectiveFrom,
            EffectiveTo    = effectiveTo,
            Priority       = priority,
            Notes          = notes,
            RuleCode       = ruleCode,
            IsActive       = true,
            Status         = DeriveStatus(effectiveFrom, effectiveTo),
        };

    public static CommissionRuleEntity CreateForCategory(
        string categoryCode, decimal rate, DateTime effectiveFrom, DateTime? effectiveTo,
        CommissionRulePriority priority, string? notes, string? ruleCode) =>
        new()
        {
            RuleType       = CommissionRuleType.Category,
            CategoryCode   = categoryCode,
            CommissionRate = rate,
            EffectiveFrom  = effectiveFrom,
            EffectiveTo    = effectiveTo,
            Priority       = priority,
            Notes          = notes,
            RuleCode       = ruleCode,
            IsActive       = true,
            Status         = DeriveStatus(effectiveFrom, effectiveTo),
        };

    public static CommissionRuleEntity CreateForPlan(
        long providerPlanId, decimal rate, DateTime effectiveFrom, DateTime? effectiveTo,
        CommissionRulePriority priority, string? notes, string? ruleCode) =>
        new()
        {
            RuleType       = CommissionRuleType.Plan,
            ProviderPlanId = providerPlanId,
            CommissionRate = rate,
            EffectiveFrom  = effectiveFrom,
            EffectiveTo    = effectiveTo,
            Priority       = priority,
            Notes          = notes,
            RuleCode       = ruleCode,
            IsActive       = true,
            Status         = DeriveStatus(effectiveFrom, effectiveTo),
        };

    public static CommissionRuleEntity CreateProviderOverride(
        long providerProfileId, decimal rate, DateTime effectiveFrom, DateTime? effectiveTo,
        CommissionRulePriority priority, string? notes, string? ruleCode) =>
        new()
        {
            RuleType          = CommissionRuleType.ProviderOverride,
            ProviderProfileId = providerProfileId,
            CommissionRate    = rate,
            EffectiveFrom     = effectiveFrom,
            EffectiveTo       = effectiveTo,
            Priority          = priority,
            Notes             = notes,
            RuleCode          = ruleCode,
            IsActive          = true,
            Status            = DeriveStatus(effectiveFrom, effectiveTo),
        };

    // ── Domain methods ────────────────────────────────────────────────────────

    public void Update(decimal rate, DateTime effectiveFrom, DateTime? effectiveTo,
        CommissionRulePriority priority, string? notes)
    {
        CommissionRate = rate;
        EffectiveFrom  = effectiveFrom;
        EffectiveTo    = effectiveTo;
        Priority       = priority;
        Notes          = notes;
        Status         = DeriveStatus(effectiveFrom, effectiveTo);
    }

    public void Deactivate()
    {
        IsActive = false;
        Status   = CommissionRuleStatus.Inactive;
    }

    /// <summary>
    /// Re-activates an Inactive rule. Status is re-derived from EffectiveFrom/EffectiveTo dates.
    /// Expired rules cannot be reactivated; the caller should extend EffectiveTo first via Update().
    /// </summary>
    public void Reactivate()
    {
        IsActive = true;
        Status   = DeriveStatus(EffectiveFrom, EffectiveTo);
    }

    public void IncrementAppliedCount() => ResolvedAppliedCount++;

    public bool IsEffective(DateTime atUtc) =>
        IsActive && EffectiveFrom <= atUtc && (EffectiveTo == null || EffectiveTo >= atUtc);

    // ── Private helpers ───────────────────────────────────────────────────────

    private static CommissionRuleStatus DeriveStatus(DateTime effectiveFrom, DateTime? effectiveTo)
    {
        var now = DateTime.UtcNow;
        if (effectiveFrom > now)     return CommissionRuleStatus.Scheduled;
        if (effectiveTo.HasValue && effectiveTo.Value < now) return CommissionRuleStatus.Expired;
        return CommissionRuleStatus.Active;
    }
}
