using Aizen.Bff.AdminPanel.Application.AdminPayment.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Command.UpdateCommissionRule;

public sealed class UpdateCommissionRuleBffCommand : AizenCommand<UpdateCommissionRuleBffCommandResponse>
{
    public long                           Id   { get; init; }
    public UpdateCommissionRuleBffRequest Body { get; init; } = default!;
}

public sealed class UpdateCommissionRuleBffCommandResponse
{
    public CommissionRuleMutateBffResult Result { get; init; } = default!;
}
