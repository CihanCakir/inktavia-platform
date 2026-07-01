using Aizen.Bff.AdminPanel.Application.AdminPayment.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Command.DeactivateCommissionRule;

public sealed class DeactivateCommissionRuleBffCommand : AizenCommand<DeactivateCommissionRuleBffCommandResponse>
{
    public long Id { get; init; }
}

public sealed class DeactivateCommissionRuleBffCommandResponse
{
    public CommissionRuleMutateBffResult Result { get; init; } = default!;
}
