using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Application.Commands.CreateCommissionRule;

public sealed class CreateCommissionRuleCommand : AizenCommand<CreateCommissionRuleResult>
{
    public CommissionRuleType     RuleType          { get; init; }
    public string?                CategoryCode      { get; init; }
    public long?                  ProviderPlanId    { get; init; }
    public long?                  ProviderProfileId { get; init; }
    public decimal                CommissionRate    { get; init; }
    public DateTime               EffectiveFrom     { get; init; }
    public DateTime?              EffectiveTo       { get; init; }
    public string?                Notes             { get; init; }
    public CommissionRulePriority Priority          { get; init; } = CommissionRulePriority.Standard;

    // ── Phase 0: CargoDry targeting dimensions (optional) ─────────────────
    /// <summary>Scope this rule to a specific transaction context. Null = all contexts.</summary>
    public TransactionContextType? ContextType    { get; init; }
    /// <summary>Scope this rule to a specific CargoDry product code. Null = all products.</summary>
    public string?                 ProductCode    { get; init; }
    /// <summary>Scope this rule to a specific sales channel. Null = all channels.</summary>
    public SalesChannel?           SalesChannel   { get; init; }

    // ── Phase 5: Rule labeling / metadata (optional) ──────────────────────
    /// <summary>Human-readable name for this rule (e.g. "Provider X Rate H1-2026").</summary>
    public string?                 RuleName       { get; init; }
    /// <summary>ISO 4217 currency code. Null = any currency.</summary>
    public string?                 CurrencyCode   { get; init; }
    /// <summary>Commercial model scope. Null = any commercial model.</summary>
    public CommercialModel?        CommercialModel { get; init; }

    // ── BE-P2: Line-level commission dimensions (§20.11, optional) ─────────
    /// <summary>Line kind scope (Labor/Part/Travel/PassThrough). Null = any line type.</summary>
    public LineType?               LineType             { get; init; }
    /// <summary>Commission eligibility scope. Null = any eligibility.</summary>
    public CommissionEligibility?  CommissionEligibility { get; init; }
}

public sealed record CreateCommissionRuleResult(long Id, string RuleCode);
