using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDryMonthlySettlementAutomationPreview;

/// <summary>
/// Returns a dry-run preview of what the monthly settlement automation would do
/// for the given target year-month, without performing any mutations.
/// Phase 6 (July 2026).
/// </summary>
[DocumentationInfo("Get CargoDry monthly settlement automation preview query",
    "Read-only preview (DryRun) of settlements eligible for automation in the target year-month. " +
    "No data is mutated. Returns per-settlement predicted actions and aggregate counts. " +
    "Phase 6 (July 2026).")]
public sealed class GetCargoDryMonthlySettlementAutomationPreviewQuery
    : AizenQuery<GetCargoDryMonthlySettlementAutomationPreviewQueryResponse>
{
    /// <summary>Target calendar month in YYYYMM format (e.g. 202606).</summary>
    public int  TargetYearMonth    { get; init; }

    /// <summary>Include AutoPreparePayment step in the preview calculation. Default: false.</summary>
    public bool AutoPreparePayment { get; init; } = false;

    /// <summary>Include AutoPrepareInvoice step in the preview calculation (requires AutoPreparePayment). Default: false.</summary>
    public bool AutoPrepareInvoice { get; init; } = false;
}

public sealed class GetCargoDryMonthlySettlementAutomationPreviewQueryResponse
{
    public CargoDrySettlementAutomationPreviewDto Preview { get; init; } = default!;
}
