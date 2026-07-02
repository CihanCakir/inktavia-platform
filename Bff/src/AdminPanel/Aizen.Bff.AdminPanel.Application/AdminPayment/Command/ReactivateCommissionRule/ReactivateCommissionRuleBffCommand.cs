using Aizen.Bff.AdminPanel.Application.AdminPayment.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Command.ReactivateCommissionRule;

public sealed class ReactivateCommissionRuleBffCommand : AizenCommand<ReactivateCommissionRuleBffCommandResponse>
{
    public long Id { get; init; }
}

public sealed class ReactivateCommissionRuleBffCommandResponse
{
    public CommissionRuleMutateBffResult Result { get; init; } = default!;
}
