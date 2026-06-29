using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.ExportCargoDryBatches;

[DocumentationInfo("Export CargoDry batches BFF query handler",
    "Streams CSV from the module batch export endpoint and returns bytes for the controller to serve.")]
public sealed class ExportCargoDryBatchesBffQueryHandler
    : AizenQueryHandler<ExportCargoDryBatchesBffQuery, CargoDryExportBffResponse>
{
    private readonly IAdminCargoDryBffRemoteCall _remote;

    public ExportCargoDryBatchesBffQueryHandler(IAdminCargoDryBffRemoteCall remote)
        => _remote = remote;

    public override async Task<CargoDryExportBffResponse> Handle(
        ExportCargoDryBatchesBffQuery request, CancellationToken ct)
    {
        var upstream    = await _remote.ExportBatchesAsync(ct);
        var bytes       = await upstream.Content.ReadAsByteArrayAsync(ct);
        var contentType = upstream.Content.Headers.ContentType?.ToString() ?? "text/csv";

        return new CargoDryExportBffResponse
        {
            Bytes       = bytes,
            ContentType = contentType,
            FileName    = $"cargodry-batches-{DateTimeOffset.UtcNow:yyyyMMdd}.csv",
        };
    }
}
