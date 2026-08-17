using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.CreateCommissionRule;

public sealed class CreateCommissionRuleBffCommand : AizenCommand<CreateCommissionRuleBffCommandResponse>
{
    public CreateCommissionRuleBffRequest Body { get; init; } = default!;
}

public sealed class CreateCommissionRuleBffCommandResponse
{
    public CommissionRuleCreateBffResult Result { get; init; } = default!;
}
