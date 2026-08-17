using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.IssueInvoice;

[DocumentationInfo("Issue invoice BFF command handler",
    "Transitions a Draft invoice to Sent/Issued status, assigns an invoice number, and triggers the InvoiceIssuedMessage event.")]
public sealed class IssueInvoiceBffCommandHandler
    : AizenCommandHandler<IssueInvoiceBffCommand, IssueInvoiceBffCommandResponse>
{
    private readonly IPaymentRemoteCall _remote;

    public IssueInvoiceBffCommandHandler(IPaymentRemoteCall remote)
        => _remote = remote;

    public override async Task<IssueInvoiceBffCommandResponse?> Handle(
        IssueInvoiceBffCommand request, CancellationToken ct)
    {
        var result = await _remote.IssueInvoiceAsync(request.Id, ct);
        return new IssueInvoiceBffCommandResponse { Result = result };
    }
}
