using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Model.Result;
using Aizen.Modules.Payment.Abstraction.Requests;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.CreatePaymentEscrow;

public sealed class CreatePaymentEscrowBffCommand : AizenCommand<CreatePaymentEscrowBffCommandResponse>
{
    public required CreateEscrowRequest Body { get; init; }
}

public sealed class CreatePaymentEscrowBffCommandResponse
{
    public CreatePaymentEscrowResult Result { get; init; } = default!;
}
