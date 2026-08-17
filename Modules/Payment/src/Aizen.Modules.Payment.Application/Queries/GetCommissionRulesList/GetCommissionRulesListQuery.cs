using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Application.Dto;

namespace Aizen.Modules.Payment.Application.Queries.GetCommissionRulesList;

public sealed class GetCommissionRulesListQuery : AizenQuery<CommissionRuleListResult>
{
    // ── Core filters ──────────────────────────────────────────────────────────
    public CommissionRuleType?     RuleType { get; init; }
    public CommissionRuleStatus?   Status   { get; init; }
    public CommissionRulePriority? Priority { get; init; }
    public int                     Page     { get; init; } = 1;
    public int                     PageSize { get; init; } = 25;

    // ── Extended filters (Phase 13) ───────────────────────────────────────────
    public TransactionContextType? ContextType       { get; init; }
    public CommercialModel?        CommercialModel   { get; init; }
    public string?                 ProductCode       { get; init; }
    public SalesChannel?           SalesChannel      { get; init; }
    public string?                 Search            { get; init; }
    public long?                   ProviderProfileId { get; init; }
    public DateTime?               EffectiveOnUtc    { get; init; }
}
