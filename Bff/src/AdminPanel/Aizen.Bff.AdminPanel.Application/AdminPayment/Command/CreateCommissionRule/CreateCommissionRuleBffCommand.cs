using Aizen.Bff.AdminPanel.Application.AdminPayment.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Command.CreateCommissionRule;

public sealed class CreateCommissionRuleBffCommand : AizenCommand<CreateCommissionRuleBffCommandResponse>
{
    public CreateCommissionRuleBffRequest Body { get; init; } = default!;
}

public sealed class CreateCommissionRuleBffCommandResponse
{
    public CommissionRuleCreateBffResult Result { get; init; } = default!;
}
