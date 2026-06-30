using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Model.Result;
using Aizen.Modules.Payment.Abstraction.Requests;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Command.CancelPaymentTransaction;

public sealed class CancelPaymentTransactionBffCommand : AizenCommand<CancelPaymentTransactionBffCommandResponse>
{
    public long                    Id   { get; init; }
    public required CancelPaymentRequest Body { get; init; }
}

public sealed class CancelPaymentTransactionBffCommandResponse
{
    public CancelPaymentResult Result { get; init; } = default!;
}
