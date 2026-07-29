using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Application.Queries.ResolveCommissionRate;

public sealed class ResolveCommissionRateQuery : AizenQuery<CommissionRateResult>
{
    public long?   ProviderProfileId { get; init; }
    public long?   ProviderPlanId    { get; init; }
    public string? CategoryCode      { get; init; }

    // ── BE-P2: optional finer dims for line-level resolution (§20.11) ─────────
    public string?                 ProductCode           { get; init; }
    public LineType?               LineType              { get; init; }
    public TransactionContextType? ContextType           { get; init; }
    public CommercialModel?        CommercialModel       { get; init; }
    public SalesChannel?           SalesChannel          { get; init; }
    public string?                 CurrencyCode          { get; init; }
    public CommissionEligibility?  CommissionEligibility { get; init; }
}

/// <summary>
/// Full resolution result (BE-P2). <see cref="ResolvedFrom"/> = the matched rule's source (RuleType) and is
/// kept for the existing BFF contract; the extra fields expose the matched rule id/code, computed specificity
/// rank, and priority for audit and snapshot (P8).
/// </summary>
public sealed record CommissionRateResult(
    decimal                Rate,
    string                 ResolvedFrom,
    long                   RuleId,
    string?                RuleCode,
    int                    SpecificityRank,
    CommissionRulePriority Priority);
