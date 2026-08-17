using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.DeactivateProviderCommissionBenefitRule;


// ─── Rule: Deactivate ────────────────────────────────────────────────────────
public sealed class DeactivateProviderCommissionBenefitRuleBffCommand : AizenCommand<DeactivateProviderCommissionBenefitRuleBffResponse>
{
    public long Id { get; init; }
}
public sealed class DeactivateProviderCommissionBenefitRuleBffResponse { public ProviderCommissionBenefitRuleMutateBffResult Result { get; init; } = default!; }
