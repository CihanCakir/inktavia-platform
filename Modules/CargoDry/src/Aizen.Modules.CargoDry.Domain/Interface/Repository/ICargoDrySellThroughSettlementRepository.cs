using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Domain.Entities;

namespace Aizen.Modules.CargoDry.Domain.Interface.Repository;

public interface ICargoDrySellThroughSettlementRepository
{
    Task<CargoDrySellThroughSettlementEntity?> GetByIdAsync(long id, CancellationToken ct);

    Task<CargoDrySellThroughSettlementEntity?> GetByCodeAsync(string settlementCode, CancellationToken ct);

    /// <summary>
    /// Finds the open (Pending) settlement for the approved grouping key:
    /// ProviderProfileId + CurrencyCode + ProductCode + Month (PeriodStartUtc + PeriodEndUtc).
    /// Used by ICargoDryCommercialActivationService to find an existing monthly settlement to append to.
    /// Phase 3.2 (July 2026): settlement grouping aligned to Provider + Currency + Product + Month.
    /// </summary>
    Task<CargoDrySellThroughSettlementEntity?> GetOpenForProviderCurrencyProductPeriodAsync(
        long              providerProfileId,
        string            currencyCode,
        string            productCode,
        DateTime          periodStartUtc,
        DateTime          periodEndUtc,
        CancellationToken ct);

    Task<(List<CargoDrySellThroughSettlementEntity> Items, int Total)> GetPagedAsync(
        long?                               providerProfileId,
        long?                               consignmentAgreementId,
        string?                             productCode,
        CargoDrySellThroughSettlementStatus? status,
        DateTime?                           periodFrom,
        DateTime?                           periodTo,
        string?                             search,
        int                                 skip,
        int                                 take,
        CancellationToken                   ct);

    /// <summary>
    /// Returns all settlements whose PeriodStartUtc falls within the given target year-month (YYYYMM).
    /// Used by the monthly settlement automation to scope settlements for a single calendar month.
    /// Phase 6 (July 2026).
    /// </summary>
    Task<List<CargoDrySellThroughSettlementEntity>> GetForYearMonthAsync(
        int                                              targetYearMonth,
        IEnumerable<CargoDrySellThroughSettlementStatus> statuses,
        CancellationToken                                ct);

    Task AddAsync(CargoDrySellThroughSettlementEntity entity, CancellationToken ct);

    Task SaveChangesAsync(CancellationToken ct);
}
