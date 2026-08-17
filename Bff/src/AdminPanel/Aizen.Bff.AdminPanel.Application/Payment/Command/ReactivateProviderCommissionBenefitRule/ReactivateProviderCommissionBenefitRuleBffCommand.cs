using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.ReactivateProviderCommissionBenefitRule;


// ─── Rule: Reactivate ────────────────────────────────────────────────────────
public sealed class ReactivateProviderCommissionBenefitRuleBffCommand : AizenCommand<ReactivateProviderCommissionBenefitRuleBffResponse>
{
    public long Id { get; init; }
}
public sealed class ReactivateProviderCommissionBenefitRuleBffResponse { public ProviderCommissionBenefitRuleMutateBffResult Result { get; init; } = default!; }
