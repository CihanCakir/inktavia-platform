using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.AdminFinance.Query.GetCargoDryRenewalReconciliationBff;

/// <summary>
/// BFF query for the CargoDry renewal reconciliation report.
/// Proxies to GET /api/v1/cargodry/finance/reconciliation/renewals.
/// Mismatch detection is performed server-side in the CargoDry module handler.
/// Phase 15 (July 2026).
/// </summary>
public sealed class GetCargoDryRenewalReconciliationBffQuery
    : AizenQuery<GetCargoDryRenewalReconciliationBffResponse>
{
    public string?          ProductCode        { get; init; }
    public long?            OwnerUserId        { get; init; }
    public long?            VesselId           { get; init; }
    public int?             Status             { get; init; }
    public int?             NotificationStatus { get; init; }
    public bool?            HasMismatches      { get; init; }
    public DateTimeOffset?  DateFrom           { get; init; }
    public DateTimeOffset?  DateTo             { get; init; }
    public int              Page               { get; init; } = 1;
    public int              PageSize           { get; init; } = 50;
}

public sealed class GetCargoDryRenewalReconciliationBffResponse
{
    public CargoDryRenewalReconciliationReportDto Report { get; init; } = default!;
}
