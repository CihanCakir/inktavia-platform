using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetInvoiceDetail;

[DocumentationInfo("Get invoice detail BFF query handler",
    "Returns invoice header fields by ID, without line items or tax breakdowns (header-only view).")]
public sealed class GetInvoiceDetailBffQueryHandler
    : AizenQueryHandler<GetInvoiceDetailBffQuery, GetInvoiceDetailBffResponse>
{
    private readonly IPaymentRemoteCall _remote;

    public GetInvoiceDetailBffQueryHandler(IPaymentRemoteCall remote)
        => _remote = remote;

    public override async Task<GetInvoiceDetailBffResponse> Handle(
        GetInvoiceDetailBffQuery request, CancellationToken ct)
    {
        var invoice = await _remote.GetInvoiceAsync(request.Id, ct);
        return new GetInvoiceDetailBffResponse { Invoice = invoice };
    }
}
