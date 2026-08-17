using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Finance.Query.ExportCargoDryCommissionRuleUsageBff;

/// <summary>
/// BFF query that downloads the full CargoDry commission rule usage report as a CSV file.
/// Applies the same filters as the paged report but uses no pagination (all rows returned).
/// Phase 16G (July 2026).
/// </summary>
public sealed class ExportCargoDryCommissionRuleUsageBffQuery
    : AizenQuery<ExportCargoDryCommissionRuleUsageBffResponse>
{
    public DateTime? DateFrom          { get; init; }
    public DateTime? DateTo            { get; init; }
    public long?     RuleId            { get; init; }
    public string?   ProductCode       { get; init; }
    public string?   SalesChannel      { get; init; }
    public long?     ProviderProfileId { get; init; }
}

public sealed class ExportCargoDryCommissionRuleUsageBffResponse
{
    public byte[] Bytes       { get; init; } = [];
    public string ContentType { get; init; } = "text/csv";
    public string FileName    { get; init; } = "export.csv";
}
