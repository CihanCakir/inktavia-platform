using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetProviderCommissionBenefitRulesList;


// ─── Rule: List ──────────────────────────────────────────────────────────────
public sealed class GetProviderCommissionBenefitRulesListBffQuery : AizenQuery<ProviderCommissionBenefitRuleListBffResult>
{
    public long?   ProviderProfileId { get; init; }
    public long?   ProviderPlanId    { get; init; }
    public string? CategoryCode      { get; init; }
    public string? CurrencyCode      { get; init; }
    public bool?   Stackable         { get; init; }
    public bool?   IsActive          { get; init; }
}
