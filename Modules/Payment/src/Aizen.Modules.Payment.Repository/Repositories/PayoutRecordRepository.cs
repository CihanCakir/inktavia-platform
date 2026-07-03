using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.Payout;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Aizen.Modules.Payment.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Payment.Repository.Repositories;

public sealed class PayoutRecordRepository : IPayoutRecordRepository
{
    private readonly PaymentDbContext _db;
    public PayoutRecordRepository(PaymentDbContext db) => _db = db;

    public Task<PayoutRecordEntity?> GetByIdAsync(long id, CancellationToken ct)
        => _db.PayoutRecords.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<PayoutRecordEntity?> GetByTransactionIdAsync(long transactionId, CancellationToken ct)
        => _db.PayoutRecords.FirstOrDefaultAsync(x => x.PaymentTransactionId == transactionId, ct);

    public Task<PayoutRecordEntity?> GetBySourceAsync(string sourceType, long sourceId, CancellationToken ct)
        => _db.PayoutRecords.FirstOrDefaultAsync(
            x => x.SourceType == sourceType && x.SourceId == sourceId && x.IsActive, ct);

    public async Task<(List<PayoutRecordEntity> Items, int Total)> GetPagedAsync(
        PayoutStatus? status, long? providerProfileId,
        DateTime? fromDate, DateTime? toDate,
        int skip, int take, CancellationToken ct)
    {
        var q = _db.PayoutRecords.AsQueryable();
        if (status.HasValue) q = q.Where(x => x.Status == status.Value);
        if (providerProfileId.HasValue) q = q.Where(x => x.ProviderProfileId == providerProfileId.Value);
        if (fromDate.HasValue) q = q.Where(x => x.RequestedAt >= fromDate.Value);
        if (toDate.HasValue) q = q.Where(x => x.RequestedAt <= toDate.Value);

        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(x => x.RequestedAt).Skip(skip).Take(take).ToListAsync(ct);
        return (items, total);
    }

    public Task<List<PayoutRecordEntity>> GetPendingAsync(CancellationToken ct)
        => _db.PayoutRecords.Where(x => x.Status == PayoutStatus.Pending).OrderBy(x => x.RequestedAt).ToListAsync(ct);

    public Task AddAsync(PayoutRecordEntity entity, CancellationToken ct)
        => _db.PayoutRecords.AddAsync(entity, ct).AsTask();

    public void Update(PayoutRecordEntity entity) => _db.PayoutRecords.Update(entity);

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}
