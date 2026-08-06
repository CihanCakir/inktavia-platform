using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.CancelPaymentTransaction;

[DocumentationInfo("Cancel payment transaction BFF command handler",
    "Cancels a payment transaction in escrow or pending state. Gateway void is triggered if the transaction was not yet captured.")]
public sealed class CancelPaymentTransactionBffCommandHandler
    : AizenCommandHandler<CancelPaymentTransactionBffCommand, CancelPaymentTransactionBffCommandResponse>
{
    private readonly IPaymentRemoteCall _remote;

    public CancelPaymentTransactionBffCommandHandler(IPaymentRemoteCall remote)
        => _remote = remote;

    public override async Task<CancelPaymentTransactionBffCommandResponse?> Handle(
        CancelPaymentTransactionBffCommand request, CancellationToken ct)
    {
        var result = await _remote.CancelTransactionAsync(request.Id, request.Body, ct);
        return new CancelPaymentTransactionBffCommandResponse { Result = result };
    }
}
