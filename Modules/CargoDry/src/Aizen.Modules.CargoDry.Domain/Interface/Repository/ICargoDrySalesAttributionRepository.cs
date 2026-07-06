using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Domain.Entities;

namespace Aizen.Modules.CargoDry.Domain.Interface.Repository;

public interface ICargoDrySalesAttributionRepository
{
    Task<CargoDrySalesAttributionEntity?> GetByIdAsync(long id, CancellationToken ct);

    Task<CargoDrySalesAttributionEntity?> GetByKitIdAsync(long kitId, CancellationToken ct);

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

    Task AddAsync(CargoDrySalesAttributionEntity entity, CancellationToken ct);

    Task SaveChangesAsync(CancellationToken ct);
}
