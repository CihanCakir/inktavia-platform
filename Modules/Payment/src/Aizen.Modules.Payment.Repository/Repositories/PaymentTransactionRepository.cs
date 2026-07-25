using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.Transaction;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Aizen.Modules.Payment.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Payment.Repository.Repositories;

public sealed class PaymentTransactionRepository : IPaymentTransactionRepository
{
    private readonly PaymentDbContext _db;
    public PaymentTransactionRepository(PaymentDbContext db) => _db = db;

    // ── Transaction queries ───────────────────────────────────────────────────

    public Task<PaymentTransactionEntity?> GetByIdAsync(long id, CancellationToken ct)
        => _db.Transactions.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<List<PaymentTransactionEntity>> GetByIdsAsync(long[] ids, CancellationToken ct)
        => _db.Transactions.Where(t => ids.Contains(t.Id)).ToListAsync(ct);

    public Task<PaymentTransactionEntity?> GetByIdWithRefundsAsync(long id, CancellationToken ct)
        => _db.Transactions
              .Include(x => x.RefundRecords)
              .FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<PaymentTransactionEntity?> GetByIdempotencyKeyAsync(string key, CancellationToken ct)
        => _db.Transactions.FirstOrDefaultAsync(x => x.IdempotencyKey == key, ct);

    public Task<PaymentTransactionEntity?> GetByGatewayReferenceAsync(string gatewayReference, CancellationToken ct)
        => _db.Transactions.FirstOrDefaultAsync(x => x.GatewayReference == gatewayReference, ct);

    public Task<PaymentTransactionEntity?> GetByContextAsync(TransactionContextType contextType, long contextId, CancellationToken ct)
        => _db.Transactions.FirstOrDefaultAsync(x => x.ContextType == contextType && x.ContextId == contextId, ct);

    public async Task<(List<PaymentTransactionEntity> Items, int Total)> GetPagedAsync(
        PaymentTransactionStatus? status, TransactionType? transactionType,
        string? gatewayProvider, DateTime? fromDate, DateTime? toDate,
        string? search, int skip, int take, CancellationToken ct)
    {
        var q = _db.Transactions.AsQueryable();

        if (status.HasValue)
            q = q.Where(x => x.Status == status.Value);
        if (transactionType.HasValue)
            q = q.Where(x => x.TransactionType == transactionType.Value);
        if (!string.IsNullOrWhiteSpace(gatewayProvider))
            q = q.Where(x => x.GatewayProvider == gatewayProvider);
        if (fromDate.HasValue)
            q = q.Where(x => x.CreateDate >= fromDate.Value);
        if (toDate.HasValue)
            q = q.Where(x => x.CreateDate <= toDate.Value);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x => x.TransactionCode.Contains(search));

        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(x => x.CreateDate).Skip(skip).Take(take).ToListAsync(ct);
        return (items, total);
    }

    public async Task<(List<PaymentTransactionEntity> Items, int Total)> GetProviderPagedAsync(
        long recipientProfileId,
        PaymentTransactionStatus? status,
        TransactionType? type,
        int skip, int take,
        CancellationToken ct)
    {
        var q = _db.Transactions
            .Where(x => x.RecipientProfileId == recipientProfileId);

        if (status.HasValue)
            q = q.Where(x => x.Status == status.Value);
        if (type.HasValue)
            q = q.Where(x => x.TransactionType == type.Value);

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(x => x.CreateDate)
            .ThenByDescending(x => x.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);
        return (items, total);
    }

    public Task<List<PaymentTransactionEntity>> GetPendingIntentOlderThanAsync(
        TimeSpan olderThan, int maxBatch, CancellationToken ct)
    {
        var cutoff = DateTime.UtcNow - olderThan;
        return _db.Transactions
            .Where(x => x.Status == PaymentTransactionStatus.PendingIntent
                     && x.CreateDate <= cutoff)
            .OrderBy(x => x.CreateDate)
            .Take(maxBatch)
            .ToListAsync(ct);
    }

    public Task<List<PaymentTransactionEntity>> GetPendingIntentInRangeAsync(
        TimeSpan olderThan, TimeSpan youngerThan, int maxBatch, CancellationToken ct)
    {
        var now      = DateTime.UtcNow;
        var ceiling  = now - olderThan;   // must be at least this old
        var floor    = now - youngerThan; // must be no older than this
        return _db.Transactions
            .Where(x => x.Status == PaymentTransactionStatus.PendingIntent
                     && x.CreateDate <= ceiling
                     && x.CreateDate > floor)
            .OrderBy(x => x.CreateDate)
            .Take(maxBatch)
            .ToListAsync(ct);
    }

    public Task<List<PaymentTransactionEntity>> GetCapturedOlderThanAsync(
        TransactionContextType contextType, TimeSpan olderThan, int maxBatch, CancellationToken ct)
    {
        var cutoff = DateTime.UtcNow - olderThan;
        return _db.Transactions
            .Where(x => x.Status == PaymentTransactionStatus.Captured
                     && x.ContextType == contextType
                     && x.CreateDate <= cutoff)
            .OrderBy(x => x.CreateDate)
            .Take(maxBatch)
            .ToListAsync(ct);
    }

    // ── Refund record queries ─────────────────────────────────────────────────

    public Task<List<TransactionRefundRecord>> GetRefundRecordsAsync(long transactionId, CancellationToken ct)
        => _db.TransactionRefunds
              .Where(r => r.PaymentTransactionId == transactionId)
              .OrderByDescending(r => r.CreateDate)
              .ToListAsync(ct);

    public Task<TransactionRefundRecord?> GetRefundRecordByIdAsync(long refundRecordId, CancellationToken ct)
        => _db.TransactionRefunds.FirstOrDefaultAsync(r => r.Id == refundRecordId, ct);

    public Task AddRefundRecordAsync(TransactionRefundRecord record, CancellationToken ct)
        => _db.TransactionRefunds.AddAsync(record, ct).AsTask();

    // ── Transaction mutations ─────────────────────────────────────────────────

    public Task AddAsync(PaymentTransactionEntity entity, CancellationToken ct)
        => _db.Transactions.AddAsync(entity, ct).AsTask();

    public void Update(PaymentTransactionEntity entity)
        => _db.Transactions.Update(entity);

    public void UpdateRange(IEnumerable<PaymentTransactionEntity> entities)
        => _db.Transactions.UpdateRange(entities);

    public Task SaveChangesAsync(CancellationToken ct)
        => _db.SaveChangesAsync(ct);
}
