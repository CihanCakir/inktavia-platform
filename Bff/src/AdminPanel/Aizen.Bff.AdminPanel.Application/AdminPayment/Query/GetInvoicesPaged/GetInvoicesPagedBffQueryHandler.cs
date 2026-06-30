using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Query.GetInvoicesPaged;

[DocumentationInfo("Get invoices paged BFF query handler",
    "Returns a paged list of invoices with optional status filter for the admin invoice management view.")]
public sealed class GetInvoicesPagedBffQueryHandler
    : AizenQueryHandler<GetInvoicesPagedBffQuery, GetInvoicesPagedBffResponse>
{
    private readonly IAdminPaymentBffRemoteCall _remote;

    public GetInvoicesPagedBffQueryHandler(IAdminPaymentBffRemoteCall remote)
        => _remote = remote;

    public override async Task<GetInvoicesPagedBffResponse> Handle(
        GetInvoicesPagedBffQuery request, CancellationToken ct)
    {
        var result = await _remote.GetInvoicesPagedAsync(request.Page, request.PageSize, request.Status, ct);
        return new GetInvoicesPagedBffResponse { Result = result };
    }
}
