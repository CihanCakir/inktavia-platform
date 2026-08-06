using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Query.GetInvoicesByBuyer;

[DocumentationInfo("Get invoices by buyer BFF query handler",
    "Returns paged invoices filtered to a specific buyer profile ID. Used in user detail pages and invoice audit views.")]
public sealed class GetInvoicesByBuyerBffQueryHandler
    : AizenQueryHandler<GetInvoicesByBuyerBffQuery, GetInvoicesByBuyerBffResponse>
{
    private readonly IPaymentRemoteCall _remote;

    public GetInvoicesByBuyerBffQueryHandler(IPaymentRemoteCall remote)
        => _remote = remote;

    public override async Task<GetInvoicesByBuyerBffResponse> Handle(
        GetInvoicesByBuyerBffQuery request, CancellationToken ct)
    {
        var result = await _remote.GetInvoicesByBuyerAsync(request.BuyerId, request.Page, request.PageSize, ct);
        return new GetInvoicesByBuyerBffResponse { Result = result };
    }
}
