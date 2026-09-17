using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Domain.Entities;

namespace Aizen.Modules.CargoDry.Domain.Interface.Repository;

public interface ICargoDrySalesAttributionRepository
{
    Task<CargoDrySalesAttributionEntity?> GetByIdAsync(long id, CancellationToken ct);

    Task<CargoDrySalesAttributionEntity?> GetByKitIdAsync(long kitId, CancellationToken ct);

    /// <summary>
    /// CargoDry supply flow: the attribution recorded for a source CARGODRY_SUPPLY service request, if any.
    /// The idempotency lookup — a non-null result means this SR's sale was already recorded (skip).
    /// </summary>
    Task<CargoDrySalesAttributionEntity?> GetBySourceServiceRequestIdAsync(long serviceRequestId, CancellationToken ct);

    Task<(List<CargoDrySalesAttributionEntity> Items, int Total)> GetPagedAsync(
        long?                          providerProfileId,
        string?                        productCode,
        string?                        batchCode,
        SalesChannel?                  salesChannel,
        CargoDryCommercialModel?       commercialModel,
        CargoDrySalesAttributionStatus? status,
        long?                          settlementId,
        DateTime?                      dateFrom,
        DateTime?                      dateTo,
        string?                        search,
        int                            skip,
        int                            take,
        CancellationToken              ct);

    /// <summary>
    /// Returns all attributions linked to a specific sell-through settlement.
    /// Phase 4A: used by ResolveMonthlySellThroughSettlementCommandHandler to load
    /// the full attribution set before recalculating totals.
    /// </summary>
    Task<IReadOnlyList<CargoDrySalesAttributionEntity>> GetBySettlementIdAsync(
        long settlementId, CancellationToken ct);

    /// <summary>
    /// Returns attributions in a settlement that are NOT yet financially resolved.
    /// Phase 4A: used by ResolveMonthlySellThroughSettlementCommandHandler to block
    /// ReadyForSettlement if any attribution still has null financial amounts.
    /// </summary>
    Task<IReadOnlyList<CargoDrySalesAttributionEntity>> GetUnresolvedBySettlementIdAsync(
        long settlementId, CancellationToken ct);

    /// <summary>
    /// Returns ConsignmentSellThrough attributions created within [periodStartUtc, periodEndUtc) that are NOT yet linked
    /// to any sell-through settlement (SellThroughSettlementId is null) and are not in a terminal status
    /// (Settled/Cancelled). Used by the monthly settlement automation's second-pass linker to heal orphaned attributions
    /// (link them to — or create — the period settlement). Ordered by CreatedAtUtc for deterministic grouping.
    /// </summary>
    Task<IReadOnlyList<CargoDrySalesAttributionEntity>> GetUnlinkedConsignmentSellThroughForPeriodAsync(
        DateTime periodStartUtc, DateTime periodEndUtc, CancellationToken ct);

    /// <summary>
    /// Returns commission rule usage rows grouped by ResolvedRuleId/Name/Source,
    /// with aggregated sale amounts and provider/platform shares.
    /// Used by the Finance Commission Rule Usage Report.
    /// Phase 15 (July 2026).
    /// </summary>
    Task<(List<CargoDryCommissionRuleUsageRowDto> Items, int Total)> GetCommissionRuleUsageGroupedAsync(
        DateTime?         dateFrom,
        DateTime?         dateTo,
        long?             ruleId,
        string?           productCode,
        string?           salesChannel,
        long?             providerProfileId,
        int               skip,
        int               take,
        CancellationToken ct);

    /// <summary>
    /// SUM(ProviderShareAmount) for a provider within a date range, excluding cancelled attributions.
    /// </summary>
    Task<decimal> SumProviderCommissionAsync(long providerProfileId, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken ct);

    /// <summary>
    /// SUM(ProviderShareAmount) grouped by ProductCode+BatchCode for a provider, excluding cancelled.
    /// Returns a dictionary keyed by (ProductCode, BatchCode).
    /// </summary>
    Task<Dictionary<(string ProductCode, string? BatchCode), decimal>> SumProviderCommissionByProductBatchAsync(
        long providerProfileId, CancellationToken ct);

    /// <summary>
    /// Distinct "yyyy-MM" (UTC) keys in [fromUtc, toUtc) where the provider has ≥1 non-cancelled attribution.
    /// Used to derive monthly sales streak (CE-6b). Read-only.
    /// </summary>
    Task<HashSet<string>> GetProviderActiveSalesMonthsAsync(
        long providerProfileId, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken ct);

    Task AddAsync(CargoDrySalesAttributionEntity entity, CancellationToken ct);

    Task SaveChangesAsync(CancellationToken ct);
}
