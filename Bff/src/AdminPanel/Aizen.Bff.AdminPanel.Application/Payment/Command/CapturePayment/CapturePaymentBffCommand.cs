using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Model.Result;
using Aizen.Modules.Payment.Abstraction.Requests;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.CapturePayment;

public sealed class CapturePaymentBffCommand : AizenCommand<CapturePaymentBffCommandResponse>
{
    public long                   Id   { get; init; }
    public required CapturePaymentRequest Body { get; init; }
}

public sealed class CapturePaymentBffCommandResponse
{
    public CapturePaymentResult Result { get; init; } = default!;
}
