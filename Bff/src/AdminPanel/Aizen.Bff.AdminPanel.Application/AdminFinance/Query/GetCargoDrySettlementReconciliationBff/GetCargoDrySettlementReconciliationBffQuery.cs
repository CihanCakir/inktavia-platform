using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.AdminFinance.Query.GetCargoDrySettlementReconciliationBff;

/// <summary>
/// BFF query for the CargoDry settlement reconciliation report.
/// Proxies to GET /api/v1/cargodry/finance/reconciliation/settlements.
/// Mismatch detection is performed server-side in the CargoDry module handler.
/// Phase 15 (July 2026).
/// </summary>
public sealed class GetCargoDrySettlementReconciliationBffQuery
    : AizenQuery<GetCargoDrySettlementReconciliationBffResponse>
{
    public long?     ProviderProfileId { get; init; }
    public string?   ProductCode       { get; init; }
    public int?      Status            { get; init; }
    public bool?     HasMismatches     { get; init; }
    public DateTime? DateFrom          { get; init; }
    public DateTime? DateTo            { get; init; }
    public int       Page              { get; init; } = 1;
    public int       PageSize          { get; init; } = 50;
}

public sealed class GetCargoDrySettlementReconciliationBffResponse
{
    public CargoDrySettlementReconciliationReportDto Report { get; init; } = default!;
}
