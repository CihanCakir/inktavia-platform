using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Model.Result;
using Aizen.Modules.Payment.Abstraction.Requests;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Command.ReversePartialRefund;

public sealed class ReversePartialRefundBffCommand : AizenCommand<ReversePartialRefundBffCommandResponse>
{
    public long                       RefundRecordId { get; init; }
    public required ReverseRefundRequest Body          { get; init; }
}

public sealed class ReversePartialRefundBffCommandResponse
{
    public ReversePartialRefundResult Result { get; init; } = default!;
}
