using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.ExportCargoDryUsageReport;

[DocumentationInfo("Export CargoDry usage report BFF query handler", "Calls the CargoDry admin export endpoint, reads the raw HTTP response stream, and returns bytes + content-type for the controller to serve as a file download.")]
public sealed class ExportCargoDryUsageReportBffQueryHandler
    : AizenQueryHandler<ExportCargoDryUsageReportBffQuery, ExportCargoDryUsageReportBffResponse>
{
    private readonly ICargoDryRemoteCall _remote;

    public ExportCargoDryUsageReportBffQueryHandler(ICargoDryRemoteCall remote)
        => _remote = remote;

    public override async Task<ExportCargoDryUsageReportBffResponse> Handle(
        ExportCargoDryUsageReportBffQuery request, CancellationToken ct)
    {
        var upstream    = await _remote.ExportUsageReportAsync(request.Format, request.DateFrom, request.DateTo, ct);
        var bytes       = await upstream.Content.ReadAsByteArrayAsync(ct);
        var contentType = upstream.Content.Headers.ContentType?.ToString() ?? "text/csv";
        var fileName    = $"cargodry-kit-usage-{DateTimeOffset.UtcNow:yyyyMMdd}.{request.Format}";

        return new ExportCargoDryUsageReportBffResponse
        {
            Bytes       = bytes,
            ContentType = contentType,
            FileName    = fileName,
        };
    }
}
