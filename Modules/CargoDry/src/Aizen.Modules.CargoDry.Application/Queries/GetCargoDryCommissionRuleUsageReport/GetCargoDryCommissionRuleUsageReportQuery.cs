using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDryCommissionRuleUsageReport;

/// <summary>
/// Returns commission rule usage aggregated from CargoDrySalesAttributionEntity.
/// Grouped by ResolvedRuleId / RuleName / RuleSource / ProductCode / SalesChannel / ProviderProfileId.
/// Phase 15 (July 2026).
/// </summary>
public sealed class GetCargoDryCommissionRuleUsageReportQuery
    : AizenQuery<GetCargoDryCommissionRuleUsageReportResponse>
{
    public DateTime?  DateFrom          { get; init; }
    public DateTime?  DateTo            { get; init; }
    public long?      RuleId            { get; init; }
    public string?    ProductCode       { get; init; }
    public string?    SalesChannel      { get; init; }
    public long?      ProviderProfileId { get; init; }
    public int        Page              { get; init; } = 1;
    public int        PageSize          { get; init; } = 50;
}

public sealed class GetCargoDryCommissionRuleUsageReportResponse
{
    public CargoDryCommissionRuleUsageReportDto Report { get; init; } = default!;
}
