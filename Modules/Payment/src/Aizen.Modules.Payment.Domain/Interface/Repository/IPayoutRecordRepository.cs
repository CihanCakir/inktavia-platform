using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.Payout;

namespace Aizen.Modules.Payment.Domain.Interface.Repository;

public interface IPayoutRecordRepository
{
    Task<PayoutRecordEntity?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<PayoutRecordEntity?> GetByTransactionIdAsync(long transactionId, CancellationToken ct = default);

    /// <summary>
    /// Returns the first active payout record matching the given source type and source Id.
    /// Used for idempotency in cross-module payout preparation flows (Phase 4B).
    /// </summary>
    Task<PayoutRecordEntity?> GetBySourceAsync(string sourceType, long sourceId, CancellationToken ct = default);

    Task<(List<PayoutRecordEntity> Items, int Total)> GetPagedAsync(
        PayoutStatus? status,
        long?         providerProfileId,
        DateTime?     fromDate,
        DateTime?     toDate,
        int skip, int take,
        CancellationToken ct = default);

    Task<List<PayoutRecordEntity>> GetPendingAsync(CancellationToken ct = default);

    Task AddAsync(PayoutRecordEntity entity, CancellationToken ct = default);
    void Update(PayoutRecordEntity entity);
    Task SaveChangesAsync(CancellationToken ct = default);
}
