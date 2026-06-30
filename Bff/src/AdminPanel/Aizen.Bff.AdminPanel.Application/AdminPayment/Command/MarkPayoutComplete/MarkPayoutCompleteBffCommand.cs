using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Model.Result;
using Aizen.Modules.Payment.Abstraction.Requests;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Command.MarkPayoutComplete;

public sealed class MarkPayoutCompleteBffCommand : AizenCommand<MarkPayoutCompleteBffCommandResponse>
{
    public long                          Id   { get; init; }
    public required MarkPayoutCompleteRequest Body { get; init; }
}

public sealed class MarkPayoutCompleteBffCommandResponse
{
    public MarkPayoutCompleteResult Result { get; init; } = default!;
}
