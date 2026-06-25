using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDryUsageReport;

public sealed class GetCargoDryUsageReportQuery : AizenQuery<GetCargoDryUsageReportResponse>
{
    public DateTimeOffset? DateFrom { get; init; }
    public DateTimeOffset? DateTo   { get; init; }
}

public sealed class GetCargoDryUsageReportResponse
{
    public CargoDryKitUsageReportDto Report { get; init; } = default!;
}
