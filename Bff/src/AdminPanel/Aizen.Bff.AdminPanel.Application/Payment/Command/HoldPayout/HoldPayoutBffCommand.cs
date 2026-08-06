using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Model.Result;
using Aizen.Modules.Payment.Abstraction.Requests;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.HoldPayout;

public sealed class HoldPayoutBffCommand : AizenCommand<HoldPayoutBffCommandResponse>
{
    public long                   Id   { get; init; }
    public required HoldPayoutRequest Body { get; init; }
}

public sealed class HoldPayoutBffCommandResponse
{
    public MarkPayoutCompleteResult Result { get; init; } = default!;
}
