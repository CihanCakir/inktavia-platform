using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDryUsageReport;

public sealed class GetCargoDryUsageReportBffQuery : AizenQuery<GetCargoDryUsageReportBffResponse>
{
    public DateTimeOffset? DateFrom { get; init; }
    public DateTimeOffset? DateTo   { get; init; }
}

public sealed class GetCargoDryUsageReportBffResponse
{
    public CargoDryKitUsageReportBffDto Report { get; init; } = default!;
}
