using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetCommissionRulesList;

public sealed class GetCommissionRulesListBffQuery : AizenQuery<GetCommissionRulesListBffResponse>
{
    // ── Core filters ──────────────────────────────────────────────────────────
    public string? RuleType  { get; init; }
    public string? Status    { get; init; }
    public string? Priority  { get; init; }
    public int     Page      { get; init; } = 1;
    public int     PageSize  { get; init; } = 20;

    // ── Extended filters (Phase 13) ───────────────────────────────────────────
    public string?   ContextType       { get; init; }
    public string?   CommercialModel   { get; init; }
    public string?   ProductCode       { get; init; }
    public string?   SalesChannel      { get; init; }
    public string?   Search            { get; init; }
    public long?     ProviderProfileId { get; init; }
    public DateTime? EffectiveOnUtc    { get; init; }
}

public sealed class GetCommissionRulesListBffResponse
{
    public CommissionRuleListBffResult Result { get; init; } = default!;
}
