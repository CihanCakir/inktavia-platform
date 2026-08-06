using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminFinance.Query.ExportCargoDryRenewalReconciliationBff;

[DocumentationInfo("Export CargoDry renewal reconciliation CSV BFF query handler",
    "Proxies to the CargoDry finance renewal export endpoint and streams the full CSV. " +
    "All filters are forwarded; no pagination is applied. Phase 16G (July 2026).")]
public sealed class ExportCargoDryRenewalReconciliationBffQueryHandler
    : AizenQueryHandler<ExportCargoDryRenewalReconciliationBffQuery, ExportCargoDryRenewalReconciliationBffResponse>
{
    private readonly ICargoDryRemoteCall _remote;

    public ExportCargoDryRenewalReconciliationBffQueryHandler(ICargoDryRemoteCall remote)
        => _remote = remote;

    public override async Task<ExportCargoDryRenewalReconciliationBffResponse> Handle(
        ExportCargoDryRenewalReconciliationBffQuery request, CancellationToken ct)
    {
        var upstream    = await _remote.ExportRenewalReconciliationAsync(
            request.ProductCode,
            request.OwnerUserId,
            request.VesselId,
            request.Status,
            request.NotificationStatus,
            request.HasMismatches,
            request.DateFrom,
            request.DateTo,
            ct);

        var bytes       = await upstream.Content.ReadAsByteArrayAsync(ct);
        var contentType = upstream.Content.Headers.ContentType?.ToString() ?? "text/csv";

        return new ExportCargoDryRenewalReconciliationBffResponse
        {
            Bytes       = bytes,
            ContentType = contentType,
            FileName    = $"cargodry-renewal-reconciliation-{DateTimeOffset.UtcNow:yyyyMMdd}.csv",
        };
    }
}
