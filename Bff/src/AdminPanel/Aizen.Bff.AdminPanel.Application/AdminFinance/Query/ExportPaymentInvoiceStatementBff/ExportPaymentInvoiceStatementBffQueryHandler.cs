using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminFinance.Query.ExportPaymentInvoiceStatementBff;

[DocumentationInfo("Export Payment invoice statement CSV BFF query handler",
    "Proxies to the Payment finance invoice statement export endpoint and streams the full CSV. " +
    "All filters are forwarded; no pagination is applied. " +
    "Sensitive gateway data is excluded at the module level. Phase 16G (July 2026).")]
public sealed class ExportPaymentInvoiceStatementBffQueryHandler
    : AizenQueryHandler<ExportPaymentInvoiceStatementBffQuery, ExportPaymentInvoiceStatementBffResponse>
{
    private readonly IPaymentRemoteCall _remote;

    public ExportPaymentInvoiceStatementBffQueryHandler(IPaymentRemoteCall remote)
        => _remote = remote;

    public override async Task<ExportPaymentInvoiceStatementBffResponse> Handle(
        ExportPaymentInvoiceStatementBffQuery request, CancellationToken ct)
    {
        var upstream    = await _remote.ExportFinanceInvoiceStatementAsync(
            request.Type,
            request.Status,
            request.SourceType,
            request.BuyerUserId,
            request.Currency,
            request.FromDate,
            request.ToDate,
            request.Search,
            request.HasMismatches,
            ct);

        var bytes       = await upstream.Content.ReadAsByteArrayAsync(ct);
        var contentType = upstream.Content.Headers.ContentType?.ToString() ?? "text/csv";

        return new ExportPaymentInvoiceStatementBffResponse
        {
            Bytes       = bytes,
            ContentType = contentType,
            FileName    = $"payment-invoice-statement-{DateTimeOffset.UtcNow:yyyyMMdd}.csv",
        };
    }
}
