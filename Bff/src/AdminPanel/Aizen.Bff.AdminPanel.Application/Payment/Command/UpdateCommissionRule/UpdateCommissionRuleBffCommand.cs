using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.UpdateCommissionRule;

public sealed class UpdateCommissionRuleBffCommand : AizenCommand<UpdateCommissionRuleBffCommandResponse>
{
    public long                           Id   { get; init; }
    public UpdateCommissionRuleBffRequest Body { get; init; } = default!;
}

public sealed class UpdateCommissionRuleBffCommandResponse
{
    public CommissionRuleMutateBffResult Result { get; init; } = default!;
}
