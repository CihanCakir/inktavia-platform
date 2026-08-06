using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.ReactivateCommissionRule;

public sealed class ReactivateCommissionRuleBffCommand : AizenCommand<ReactivateCommissionRuleBffCommandResponse>
{
    public long Id { get; init; }
}

public sealed class ReactivateCommissionRuleBffCommandResponse
{
    public CommissionRuleMutateBffResult Result { get; init; } = default!;
}
