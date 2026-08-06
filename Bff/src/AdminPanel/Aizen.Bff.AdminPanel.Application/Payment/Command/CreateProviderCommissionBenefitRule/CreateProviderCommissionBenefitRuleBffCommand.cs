using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.CreateProviderCommissionBenefitRule;


// ─── Rule: Create ────────────────────────────────────────────────────────────
public sealed class CreateProviderCommissionBenefitRuleBffCommand : AizenCommand<CreateProviderCommissionBenefitRuleBffResponse>
{
    public CreateProviderCommissionBenefitRuleBffRequest Body { get; init; } = default!;
}
public sealed class CreateProviderCommissionBenefitRuleBffResponse { public ProviderCommissionBenefitRuleCreateBffResult Result { get; init; } = default!; }
