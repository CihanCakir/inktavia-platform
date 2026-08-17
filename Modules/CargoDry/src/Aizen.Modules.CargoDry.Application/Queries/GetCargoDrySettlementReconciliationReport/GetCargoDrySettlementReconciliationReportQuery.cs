using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDrySettlementReconciliationReport;

/// <summary>
/// Returns a paged settlement-payout reconciliation report.
/// Mismatch flags are computed server-side from entity field inspection only.
/// No cross-module calls. Phase 15 (July 2026).
/// </summary>
public sealed class GetCargoDrySettlementReconciliationReportQuery
    : AizenQuery<GetCargoDrySettlementReconciliationReportResponse>
{
    public long?                                providerProfileId  { get; init; }
    public string?                              ProductCode        { get; init; }
    public CargoDrySellThroughSettlementStatus? Status             { get; init; }

    /// <summary>When true, only rows with at least one MismatchFlag are returned.</summary>
    public bool?                                HasMismatches      { get; init; }

    public DateTime? DateFrom  { get; init; }
    public DateTime? DateTo    { get; init; }
    public int       Page      { get; init; } = 1;
    public int       PageSize  { get; init; } = 50;
}

public sealed class GetCargoDrySettlementReconciliationReportResponse
{
    public CargoDrySettlementReconciliationReportDto Report { get; init; } = default!;
}
