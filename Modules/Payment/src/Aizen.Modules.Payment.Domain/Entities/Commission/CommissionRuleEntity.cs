using Aizen.Core.Domain;
using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Domain.Entities.Commission;

[DocumentationInfo("Commission rule entity",
    "Data-driven commission rate. Resolution precedence: ProviderOverride > Plan > Category > Global. " +
    "Extended with RuleCode, Status, Priority, ResolvedAppliedCount for admin panel management. " +
    "Phase 0 (July 2026): Added ContextType, ProductCode, SalesChannel for CargoDry channel-specific rates. " +
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

    // ── CargoDry targeting dimensions (Phase 0, July 2026) ───────────────────
    /// <summary>
    /// Narrows this rule to a specific transaction context.
    /// Null = applies to all contexts.
    /// Use TransactionContextType.CargoDry to scope to CargoDry kit sales.
    /// </summary>
    public TransactionContextType? ContextType  { get; private set; }

    /// <summary>
    /// CargoDry product code this rule applies to.
    /// Null = applies to all products within the context.
    /// </summary>
    public string?                 ProductCode  { get; private set; }

    /// <summary>
    /// CargoDry sales channel this rule applies to.
    /// Null = applies to all channels within the context.
    /// </summary>
    public SalesChannel?           SalesChannel { get; private set; }

    // ── Phase 5: Rule resolution enrichment (July 2026) ──────────────────────
    /// <summary>
    /// Human-readable name for this rule (e.g. "Provider X Rate 2026-H1").
    /// Stored in the rule trace on CargoDrySalesAttributionEntity for auditability.
    /// Null for legacy rules created before Phase 5.
    /// </summary>
    public string?          RuleName       { get; private set; }

    /// <summary>
    /// ISO 4217 currency code this rule applies to.
    /// Null = applies to any currency within the context.
    /// Used for multi-currency CargoDry markets.
    /// </summary>
    public string?          CurrencyCode   { get; private set; }

    /// <summary>
    /// Commercial model this rule is scoped to.
    /// Null = applies to any commercial model within the context.
    /// Maps to CargoDryCommercialModel (integer values are identical).
    /// </summary>
    public CommercialModel? CommercialModel { get; private set; }

    // ── Line-level commission dimensions (BE-P2, §20.11) ─────────────────────
    /// <summary>
    /// Line kind this rule applies to (Labor/Part/Travel/PassThrough). Null = any line type.
    /// A rule with a non-null LineType only matches a request that supplies the same LineType.
    /// </summary>
    public LineType?              LineType              { get; private set; }

    /// <summary>
    /// Commission eligibility this rule applies to (Commissionable/NonCommissionable). Null = any.
    /// Lets a non-commissionable pass-through line resolve to a different rule than a commissionable line.
    /// </summary>
    public CommissionEligibility? CommissionEligibility { get; private set; }

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

    // ── Phase 5: CargoDry-specific factory methods ────────────────────────────

    /// <summary>
    /// Creates a provider-specific CargoDry commission rule (priority tier 2).
    /// Sets ContextType = CargoDry so it is picked up only by the CargoDry rule resolver.
    /// Phase 5 (July 2026).
    /// </summary>
    public static CommissionRuleEntity CreateForCargoDryProviderSpecific(
        long                   providerProfileId,
        decimal                rate,
        DateTime               effectiveFrom,
        DateTime?              effectiveTo,
        CommissionRulePriority priority,
        string?                notes,
        string?                ruleCode,
        string?                ruleName       = null,
        string?                currencyCode   = null,
        CommercialModel?       commercialModel = null) =>
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
            RuleName          = ruleName,
            CurrencyCode      = currencyCode?.ToUpperInvariant(),
            CommercialModel   = commercialModel,
            ContextType       = TransactionContextType.CargoDry,
            IsActive          = true,
            Status            = DeriveStatus(effectiveFrom, effectiveTo),
        };

    /// <summary>
    /// Creates a product+channel CargoDry commission rule (priority tier 3).
    /// Scoped by ProductCode and SalesChannel within ContextType = CargoDry.
    /// Phase 5 (July 2026).
    /// </summary>
    public static CommissionRuleEntity CreateForCargoDryProductChannel(
        string                 productCode,
        SalesChannel           salesChannel,
        decimal                rate,
        DateTime               effectiveFrom,
        DateTime?              effectiveTo,
        CommissionRulePriority priority,
        string?                notes,
        string?                ruleCode,
        string?                ruleName       = null,
        string?                currencyCode   = null,
        CommercialModel?       commercialModel = null) =>
        new()
        {
            RuleType        = CommissionRuleType.Category,
            ProductCode     = productCode.ToUpperInvariant(),
            SalesChannel    = salesChannel,
            CommissionRate  = rate,
            EffectiveFrom   = effectiveFrom,
            EffectiveTo     = effectiveTo,
            Priority        = priority,
            Notes           = notes,
            RuleCode        = ruleCode,
            RuleName        = ruleName,
            CurrencyCode    = currencyCode?.ToUpperInvariant(),
            CommercialModel = commercialModel,
            ContextType     = TransactionContextType.CargoDry,
            IsActive        = true,
            Status          = DeriveStatus(effectiveFrom, effectiveTo),
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

    /// <summary>
    /// Sets optional Phase 5 labeling fields after any factory method.
    /// Called by CreateCommissionRuleCommandHandler when the admin supplies these fields.
    /// Phase 13 (July 2026): exposed for general use — not just CargoDry-specific factory methods.
    /// </summary>
    public void SetMetadata(string? ruleName, string? currencyCode, CommercialModel? commercialModel)
    {
        RuleName        = ruleName;
        CurrencyCode    = currencyCode?.ToUpperInvariant();
        CommercialModel = commercialModel;
    }

    /// <summary>
    /// Sets Phase 0 CargoDry targeting dimensions after any factory method.
    /// Allows scoping a Global/Category/Plan/ProviderOverride rule to a specific context, product, or channel.
    /// Phase 13 (July 2026): exposed for general use — not just CargoDry-specific factory methods.
    /// </summary>
    public void SetContextDimensions(
        TransactionContextType? contextType,
        string?                 productCode,
        SalesChannel?           salesChannel)
    {
        ContextType  = contextType;
        ProductCode  = productCode?.ToUpperInvariant();
        SalesChannel = salesChannel;
    }

    /// <summary>
    /// Sets the line-level commission dimensions (BE-P2, §20.11) after any factory method.
    /// Allows scoping a rule to a specific LineType and/or CommissionEligibility.
    /// </summary>
    public void SetLineDimensions(LineType? lineType, CommissionEligibility? commissionEligibility)
    {
        LineType              = lineType;
        CommissionEligibility = commissionEligibility;
    }

    /// <summary>
    /// Sets the combined primary scope (Provider / Plan / Category) that drives the 8-level specificity
    /// matrix (§13.7). Unlike the single-dimension factory methods, this lets a rule declare more than one
    /// primary dimension at once (e.g. a Provider+Plan+Category rule), which the resolver ranks above the
    /// narrower single-dimension rules. Only the supplied (non-omitted) dimensions are changed.
    /// </summary>
    public void SetPrimaryScope(long? providerProfileId, long? providerPlanId, string? categoryCode)
    {
        ProviderProfileId = providerProfileId;
        ProviderPlanId    = providerPlanId;
        CategoryCode      = categoryCode;
    }

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
