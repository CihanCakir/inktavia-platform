using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.UpdateProviderCommissionBenefitRule;


// ─── Rule: Update ────────────────────────────────────────────────────────────
public sealed class UpdateProviderCommissionBenefitRuleBffCommand : AizenCommand<UpdateProviderCommissionBenefitRuleBffResponse>
{
    public long                                            Id   { get; init; }
    public UpdateProviderCommissionBenefitRuleBffRequest   Body { get; init; } = default!;
}
public sealed class UpdateProviderCommissionBenefitRuleBffResponse { public ProviderCommissionBenefitRuleMutateBffResult Result { get; init; } = default!; }
