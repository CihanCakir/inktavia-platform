using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Query.ExportCargoDryUsageReport;

public sealed class ExportCargoDryUsageReportBffQuery : AizenQuery<ExportCargoDryUsageReportBffResponse>
{
    public string          Format   { get; init; } = "csv";
    public DateTimeOffset? DateFrom { get; init; }
    public DateTimeOffset? DateTo   { get; init; }
}

public sealed class ExportCargoDryUsageReportBffResponse
{
    public byte[] Bytes       { get; init; } = [];
    public string ContentType { get; init; } = "text/csv";
    public string FileName    { get; init; } = default!;
}
