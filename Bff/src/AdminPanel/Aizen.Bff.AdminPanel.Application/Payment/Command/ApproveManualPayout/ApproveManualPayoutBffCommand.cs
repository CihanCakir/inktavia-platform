using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Model.Result;
using Aizen.Modules.Payment.Abstraction.Requests;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.ApproveManualPayout;

public sealed class ApproveManualPayoutBffCommand : AizenCommand<ApproveManualPayoutBffCommandResponse>
{
    public long                           Id   { get; init; }
    public required ApproveManualPayoutRequest Body { get; init; }
}

public sealed class ApproveManualPayoutBffCommandResponse
{
    public MarkPayoutCompleteResult Result { get; init; } = default!;
}
