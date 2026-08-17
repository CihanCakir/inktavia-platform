using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Domain.Entities;

namespace Aizen.Modules.CargoDry.Domain.Interface.Repository;

/// <summary>
/// Repository interface for CargoDry monthly settlement automation run records.
/// Phase 6 (July 2026).
/// </summary>
public interface ICargoDrySettlementAutomationRunRepository
{
    Task<CargoDrySettlementAutomationRunEntity?> GetByIdAsync(long id, CancellationToken ct);

    Task<CargoDrySettlementAutomationRunEntity?> GetByCodeAsync(string runCode, CancellationToken ct);

    Task<(List<CargoDrySettlementAutomationRunEntity> Items, int Total)> GetPagedAsync(
        int?                                    targetYearMonth,
        CargoDrySettlementAutomationRunStatus?  status,
        CargoDrySettlementAutomationMode?       mode,
        long?                                   triggeredByUserId,
        DateTime?                               fromUtc,
        DateTime?                               toUtc,
        int                                     skip,
        int                                     take,
        CancellationToken                       ct);

    /// <summary>Returns the next sequential run number for the given targetYearMonth to build RunCode.</summary>
    Task<int> GetNextRunSequenceAsync(int targetYearMonth, CancellationToken ct);

    Task AddAsync(CargoDrySettlementAutomationRunEntity entity, CancellationToken ct);

    Task SaveChangesAsync(CancellationToken ct);
}
