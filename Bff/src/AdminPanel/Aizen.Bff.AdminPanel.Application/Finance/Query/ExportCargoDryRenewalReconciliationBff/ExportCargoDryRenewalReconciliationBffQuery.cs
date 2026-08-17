using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Finance.Query.ExportCargoDryRenewalReconciliationBff;

/// <summary>
/// BFF query that downloads the full CargoDry renewal reconciliation report as a CSV file.
/// Applies the same filters as the paged report but uses no pagination (all rows returned).
/// Phase 16G (July 2026).
/// </summary>
public sealed class ExportCargoDryRenewalReconciliationBffQuery
    : AizenQuery<ExportCargoDryRenewalReconciliationBffResponse>
{
    public string?          ProductCode        { get; init; }
    public long?            OwnerUserId        { get; init; }
    public long?            VesselId           { get; init; }
    public int?             Status             { get; init; }
    public int?             NotificationStatus { get; init; }
    public bool?            HasMismatches      { get; init; }
    public DateTimeOffset?  DateFrom           { get; init; }
    public DateTimeOffset?  DateTo             { get; init; }
}

public sealed class ExportCargoDryRenewalReconciliationBffResponse
{
    public byte[] Bytes       { get; init; } = [];
    public string ContentType { get; init; } = "text/csv";
    public string FileName    { get; init; } = "export.csv";
}
