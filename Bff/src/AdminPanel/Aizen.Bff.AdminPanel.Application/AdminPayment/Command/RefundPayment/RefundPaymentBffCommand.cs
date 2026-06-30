using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Model.Result;
using Aizen.Modules.Payment.Abstraction.Requests;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Command.RefundPayment;

public sealed class RefundPaymentBffCommand : AizenCommand<RefundPaymentBffCommandResponse>
{
    public long                     Id   { get; init; }
    public required RefundPaymentRequest Body { get; init; }
}

public sealed class RefundPaymentBffCommandResponse
{
    public RefundPaymentResult Result { get; init; } = default!;
}
