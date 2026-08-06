using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Query.GetInvoiceFull;

[DocumentationInfo("Get invoice full BFF query handler",
    "Returns a full invoice record by ID, including all line items and tax breakdowns. Used for invoice detail and PDF generation.")]
public sealed class GetInvoiceFullBffQueryHandler
    : AizenQueryHandler<GetInvoiceFullBffQuery, GetInvoiceFullBffResponse>
{
    private readonly IPaymentRemoteCall _remote;

    public GetInvoiceFullBffQueryHandler(IPaymentRemoteCall remote)
        => _remote = remote;

    public override async Task<GetInvoiceFullBffResponse> Handle(
        GetInvoiceFullBffQuery request, CancellationToken ct)
    {
        var invoice = await _remote.GetInvoiceFullAsync(request.Id, ct);
        return new GetInvoiceFullBffResponse { Invoice = invoice };
    }
}
