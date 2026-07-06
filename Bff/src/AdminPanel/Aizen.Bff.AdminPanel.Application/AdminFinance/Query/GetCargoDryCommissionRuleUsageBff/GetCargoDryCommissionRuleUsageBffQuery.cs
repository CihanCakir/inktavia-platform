using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.AdminFinance.Query.GetCargoDryCommissionRuleUsageBff;

/// <summary>
/// BFF query for the CargoDry commission rule usage report.
/// Proxies to GET /api/v1/cargodry/finance/reports/commission-rule-usage.
/// Aggregation is performed server-side (DB-level GroupBy) in the CargoDry module handler.
/// Phase 15 (July 2026).
/// </summary>
public sealed class GetCargoDryCommissionRuleUsageBffQuery
    : AizenQuery<GetCargoDryCommissionRuleUsageBffResponse>
{
    public DateTime? DateFrom          { get; init; }
    public DateTime? DateTo            { get; init; }
    public long?     RuleId            { get; init; }
    public string?   ProductCode       { get; init; }
    public string?   SalesChannel      { get; init; }
    public long?     ProviderProfileId { get; init; }
    public int       Page              { get; init; } = 1;
    public int       PageSize          { get; init; } = 50;
}

public sealed class GetCargoDryCommissionRuleUsageBffResponse
{
    public CargoDryCommissionRuleUsageReportDto Report { get; init; } = default!;
}
