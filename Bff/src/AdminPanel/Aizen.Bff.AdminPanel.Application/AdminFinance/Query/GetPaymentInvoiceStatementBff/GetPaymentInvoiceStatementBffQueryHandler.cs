using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminFinance.Query.GetPaymentInvoiceStatementBff;

[DocumentationInfo("Get payment finance invoice statement report BFF query handler",
    "Proxies the admin finance invoice statement request to the Payment module finance endpoint. " +
    "Returns a paged invoice statement report with server-side mismatch detection and currency-level summaries. " +
    "No financial calculations are performed in the BFF. " +
    "Phase 15 (July 2026).")]
public sealed class GetPaymentInvoiceStatementBffQueryHandler
    : AizenQueryHandler<GetPaymentInvoiceStatementBffQuery, GetPaymentInvoiceStatementBffResponse>
{
    private readonly IAdminPaymentBffRemoteCall _remote;

    public GetPaymentInvoiceStatementBffQueryHandler(IAdminPaymentBffRemoteCall remote)
        => _remote = remote;

    public override async Task<GetPaymentInvoiceStatementBffResponse> Handle(
        GetPaymentInvoiceStatementBffQuery request, CancellationToken ct)
    {
        var result = await _remote.GetFinanceInvoiceStatementAsync(
            request.Type,
            request.Status,
            request.SourceType,
            request.BuyerUserId,
            request.Currency,
            request.FromDate,
            request.ToDate,
            request.Search,
            request.HasMismatches,
            request.Page,
            request.PageSize,
            ct);

        return new GetPaymentInvoiceStatementBffResponse { Report = result };
    }
}
