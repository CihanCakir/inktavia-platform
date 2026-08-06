using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Command.CreateInvoiceDraft;

[DocumentationInfo("Create invoice draft BFF command handler",
    "Creates a new invoice in Draft status with header, line items, and tax breakdowns. Draft invoices are editable before issuance.")]
public sealed class CreateInvoiceDraftBffCommandHandler
    : AizenCommandHandler<CreateInvoiceDraftBffCommand, CreateInvoiceDraftBffCommandResponse>
{
    private readonly IPaymentRemoteCall _remote;

    public CreateInvoiceDraftBffCommandHandler(IPaymentRemoteCall remote)
        => _remote = remote;

    public override async Task<CreateInvoiceDraftBffCommandResponse?> Handle(
        CreateInvoiceDraftBffCommand request, CancellationToken ct)
    {
        var result = await _remote.CreateInvoiceDraftAsync(request.Body, ct);
        return new CreateInvoiceDraftBffCommandResponse { Result = result };
    }
}
