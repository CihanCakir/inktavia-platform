using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminFinance.Query.ExportCargoDrySettlementReconciliationBff;

/// <summary>
/// BFF query that downloads the full CargoDry settlement reconciliation report as a CSV file.
/// Applies the same filters as the paged report but uses no pagination (all rows returned).
/// Phase 16G (July 2026).
/// </summary>
public sealed class ExportCargoDrySettlementReconciliationBffQuery
    : AizenQuery<ExportCargoDrySettlementReconciliationBffResponse>
{
    public long?     ProviderProfileId { get; init; }
    public string?   ProductCode       { get; init; }
    public int?      Status            { get; init; }
    public bool?     HasMismatches     { get; init; }
    public DateTime? DateFrom          { get; init; }
    public DateTime? DateTo            { get; init; }
}

public sealed class ExportCargoDrySettlementReconciliationBffResponse
{
    public byte[] Bytes       { get; init; } = [];
    public string ContentType { get; init; } = "text/csv";
    public string FileName    { get; init; } = "export.csv";
}
