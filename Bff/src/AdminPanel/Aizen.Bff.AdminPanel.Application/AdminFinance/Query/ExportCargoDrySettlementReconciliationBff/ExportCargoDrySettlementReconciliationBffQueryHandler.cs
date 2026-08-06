using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminFinance.Query.ExportCargoDrySettlementReconciliationBff;

[DocumentationInfo("Export CargoDry settlement reconciliation CSV BFF query handler",
    "Proxies to the CargoDry finance export endpoint and streams the full CSV. " +
    "All filters are forwarded; no pagination is applied. Phase 16G (July 2026).")]
public sealed class ExportCargoDrySettlementReconciliationBffQueryHandler
    : AizenQueryHandler<ExportCargoDrySettlementReconciliationBffQuery, ExportCargoDrySettlementReconciliationBffResponse>
{
    private readonly ICargoDryRemoteCall _remote;

    public ExportCargoDrySettlementReconciliationBffQueryHandler(ICargoDryRemoteCall remote)
        => _remote = remote;

    public override async Task<ExportCargoDrySettlementReconciliationBffResponse> Handle(
        ExportCargoDrySettlementReconciliationBffQuery request, CancellationToken ct)
    {
        var upstream    = await _remote.ExportSettlementReconciliationAsync(
            request.ProviderProfileId,
            request.ProductCode,
            request.Status,
            request.HasMismatches,
            request.DateFrom,
            request.DateTo,
            ct);

        var bytes       = await upstream.Content.ReadAsByteArrayAsync(ct);
        var contentType = upstream.Content.Headers.ContentType?.ToString() ?? "text/csv";

        return new ExportCargoDrySettlementReconciliationBffResponse
        {
            Bytes       = bytes,
            ContentType = contentType,
            FileName    = $"cargodry-settlement-reconciliation-{DateTimeOffset.UtcNow:yyyyMMdd}.csv",
        };
    }
}
