using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Model.Result;
using Aizen.Modules.Payment.Abstraction.Requests;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Command.ReinstatePayment;

public sealed class ReinstatePaymentBffCommand : AizenCommand<ReinstatePaymentBffCommandResponse>
{
    public long                        Id   { get; init; }
    public required ReinstatePaymentRequest Body { get; init; }
}

public sealed class ReinstatePaymentBffCommandResponse
{
    public ReinstateCancelledTransactionResult Result { get; init; } = default!;
}
