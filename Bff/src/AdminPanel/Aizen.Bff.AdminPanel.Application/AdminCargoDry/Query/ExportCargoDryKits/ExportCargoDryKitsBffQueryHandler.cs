using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.ExportCargoDryKits;

[DocumentationInfo("Export CargoDry kits BFF query handler",
    "Streams CSV from the module kit export endpoint and returns bytes for the controller to serve.")]
public sealed class ExportCargoDryKitsBffQueryHandler
    : AizenQueryHandler<ExportCargoDryKitsBffQuery, CargoDryExportBffResponse>
{
    private readonly IAdminCargoDryBffRemoteCall _remote;

    public ExportCargoDryKitsBffQueryHandler(IAdminCargoDryBffRemoteCall remote)
        => _remote = remote;

    public override async Task<CargoDryExportBffResponse> Handle(
        ExportCargoDryKitsBffQuery request, CancellationToken ct)
    {
        var upstream    = await _remote.ExportKitsAsync(
            request.Status, request.Search, request.VesselId, request.BatchCode, ct);
        var bytes       = await upstream.Content.ReadAsByteArrayAsync(ct);
        var contentType = upstream.Content.Headers.ContentType?.ToString() ?? "text/csv";

        return new CargoDryExportBffResponse
        {
            Bytes       = bytes,
            ContentType = contentType,
            FileName    = $"cargodry-kits-{DateTimeOffset.UtcNow:yyyyMMdd}.csv",
        };
    }
}
