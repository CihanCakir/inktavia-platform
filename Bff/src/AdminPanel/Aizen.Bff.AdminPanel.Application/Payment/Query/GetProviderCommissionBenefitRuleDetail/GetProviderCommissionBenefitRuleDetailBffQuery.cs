using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetProviderCommissionBenefitRuleDetail;


// ─── Rule: Detail ────────────────────────────────────────────────────────────
public sealed class GetProviderCommissionBenefitRuleDetailBffQuery : AizenQuery<ProviderCommissionBenefitRuleDetailBffResponse>
{
    public long Id { get; init; }
}
public sealed class ProviderCommissionBenefitRuleDetailBffResponse { public ProviderCommissionBenefitRuleDetailBffDto? Result { get; init; } }
