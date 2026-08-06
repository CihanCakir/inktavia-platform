using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Model.Result;
using Aizen.Modules.Payment.Abstraction.Requests;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.ReleasePaymentEscrow;

public sealed class ReleasePaymentEscrowBffCommand : AizenCommand<ReleasePaymentEscrowBffCommandResponse>
{
    public long                      Id   { get; init; }
    public required ReleaseEscrowRequest Body { get; init; }
}

public sealed class ReleasePaymentEscrowBffCommandResponse
{
    public ReleasePaymentEscrowResult Result { get; init; } = default!;
}
