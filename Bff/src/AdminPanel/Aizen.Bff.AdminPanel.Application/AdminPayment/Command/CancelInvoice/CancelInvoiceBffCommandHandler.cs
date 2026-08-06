using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Command.CancelInvoice;

[DocumentationInfo("Cancel invoice BFF command handler",
    "Cancels a Draft or Sent invoice, marking it as Cancelled and preventing further payment attempts against it.")]
public sealed class CancelInvoiceBffCommandHandler
    : AizenCommandHandler<CancelInvoiceBffCommand, CancelInvoiceBffCommandResponse>
{
    private readonly IPaymentRemoteCall _remote;

    public CancelInvoiceBffCommandHandler(IPaymentRemoteCall remote)
        => _remote = remote;

    public override async Task<CancelInvoiceBffCommandResponse?> Handle(
        CancelInvoiceBffCommand request, CancellationToken ct)
    {
        var result = await _remote.CancelInvoiceAsync(request.Id, ct);
        return new CancelInvoiceBffCommandResponse { Result = result };
    }
}
