using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDryRenewalReconciliationReport;

/// <summary>
/// Returns a paged renewal billing reconciliation report.
/// Mismatch flags are computed server-side from entity field inspection only.
/// No cross-module calls. Phase 15 (July 2026).
/// </summary>
public sealed class GetCargoDryRenewalReconciliationReportQuery
    : AizenQuery<GetCargoDryRenewalReconciliationReportResponse>
{
    public string?                             ProductCode        { get; init; }
    public long?                               OwnerUserId        { get; init; }
    public long?                               VesselId           { get; init; }
    public CargoDryRenewalPreparationStatus?   Status             { get; init; }
    public CargoDryRenewalNotificationStatus?  NotificationStatus { get; init; }

    /// <summary>When true, only rows with at least one MismatchFlag are returned.</summary>
    public bool?     HasMismatches { get; init; }

    public DateTimeOffset? DateFrom { get; init; }
    public DateTimeOffset? DateTo   { get; init; }
    public int             Page     { get; init; } = 1;
    public int             PageSize { get; init; } = 50;
}

public sealed class GetCargoDryRenewalReconciliationReportResponse
{
    public CargoDryRenewalReconciliationReportDto Report { get; init; } = default!;
}
