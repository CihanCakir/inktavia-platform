using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.Transaction;

namespace Aizen.Modules.Payment.Domain.Interface.Repository;

public interface IPaymentTransactionRepository
{
    // ── Transaction queries ───────────────────────────────────────────────────

    Task<PaymentTransactionEntity?> GetByIdAsync(long id, CancellationToken ct = default);

    /// <summary>
    /// Returns transactions for the given set of IDs in a single query.
    /// Used by GetPayoutListQueryHandler to batch-enrich payout breakdown fields.
    /// </summary>
    Task<List<PaymentTransactionEntity>> GetByIdsAsync(long[] ids, CancellationToken ct = default);

    Task<PaymentTransactionEntity?> GetByIdempotencyKeyAsync(string key, CancellationToken ct = default);
    Task<PaymentTransactionEntity?> GetByGatewayReferenceAsync(string gatewayReference, CancellationToken ct = default);
    Task<PaymentTransactionEntity?> GetByContextAsync(TransactionContextType contextType, long contextId, CancellationToken ct = default);

    /// <summary>
    /// Returns the transaction with its RefundRecords collection eagerly loaded.
    /// Use in refund command handlers that need to call ApplyRefund or ReverseRefund.
    /// </summary>
    Task<PaymentTransactionEntity?> GetByIdWithRefundsAsync(long id, CancellationToken ct = default);

    Task<(List<PaymentTransactionEntity> Items, int Total)> GetPagedAsync(
        PaymentTransactionStatus? status,
        TransactionType?          transactionType,
        string?                   gatewayProvider,
        DateTime?                 fromDate,
        DateTime?                 toDate,
        string?                   search,
        int skip, int take,
        CancellationToken ct = default);

    /// <summary>
    /// Returns paged transactions where the provider is the recipient.
    /// Used by the provider-facing transaction list (PAY-3).
    /// </summary>
    Task<(List<PaymentTransactionEntity> Items, int Total)> GetProviderPagedAsync(
        long recipientProfileId,
        PaymentTransactionStatus? status,
        TransactionType?          type,
        int skip, int take,
        DateTime? fromUtc = null, DateTime? toUtc = null,
        CancellationToken ct = default);

    /// <summary>
    /// Returns PendingIntent transactions older than <paramref name="olderThan"/>, capped at <paramref name="maxBatch"/>.
    /// Used by PaymentEscrowTimeoutJob and StaleEscrowCleanupJob.
    /// </summary>
    Task<List<PaymentTransactionEntity>> GetPendingIntentOlderThanAsync(
        TimeSpan olderThan,
        int maxBatch = 500,
        CancellationToken ct = default);

    /// <summary>
    /// Returns PendingIntent transactions whose age is strictly between
    /// <paramref name="olderThan"/> and <paramref name="youngerThan"/>.
    /// Used by PaymentWebhookRetryJob to find transactions that should have received a
    /// gateway webhook but did not — without overlapping the EscrowTimeoutJob window.
    /// </summary>
    Task<List<PaymentTransactionEntity>> GetPendingIntentInRangeAsync(
        TimeSpan olderThan,
        TimeSpan youngerThan,
        int maxBatch = 100,
        CancellationToken ct = default);

    /// <summary>
    /// Returns Captured transactions for a specific <paramref name="contextType"/> that are
    /// older than <paramref name="olderThan"/>. Used by PaymentAutoReleaseEligibilityJob
    /// to detect stuck escrow that the SR module never resolved.
    /// </summary>
    Task<List<PaymentTransactionEntity>> GetCapturedOlderThanAsync(
        TransactionContextType contextType,
        TimeSpan olderThan,
        int maxBatch = 100,
        CancellationToken ct = default);

    // ── Refund record queries ─────────────────────────────────────────────────

    /// <summary>
    /// Returns all refund records for a transaction, ordered newest-first.
    /// Used by GetTransactionRefundHistoryQuery.
    /// </summary>
    Task<List<TransactionRefundRecord>> GetRefundRecordsAsync(long transactionId, CancellationToken ct = default);

    /// <summary>
    /// Fetches a single refund record by its primary key.
    /// Used by ReversePartialRefundCommandHandler.
    /// </summary>
    Task<TransactionRefundRecord?> GetRefundRecordByIdAsync(long refundRecordId, CancellationToken ct = default);

    /// <summary>
    /// Persists a newly created TransactionRefundRecord (Pending state).
    /// Must be followed by SaveChangesAsync.
    /// </summary>
    Task AddRefundRecordAsync(TransactionRefundRecord record, CancellationToken ct = default);

    /// <summary>
    /// WS1 idempotency guard: true if a non-Failed FULL refund (RefundType.Full) already exists for the
    /// transaction. Mirrors the partial-unique DB index (PaymentTransactionId WHERE RefundType=1 AND
    /// Status&lt;&gt;Failed) — used as the commit-level short-circuit in ServiceRequestCancelledConsumer.
    /// </summary>
    Task<bool> FullRefundExistsAsync(long transactionId, CancellationToken ct = default);

    // ── Transaction mutations ─────────────────────────────────────────────────

    Task AddAsync(PaymentTransactionEntity entity, CancellationToken ct = default);
    void Update(PaymentTransactionEntity entity);
    void UpdateRange(IEnumerable<PaymentTransactionEntity> entities);
    Task SaveChangesAsync(CancellationToken ct = default);
}
