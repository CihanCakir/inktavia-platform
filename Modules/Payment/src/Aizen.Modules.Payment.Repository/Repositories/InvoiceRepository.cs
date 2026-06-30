using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.Invoice;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Aizen.Modules.Payment.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Payment.Repository.Repositories;

public sealed class InvoiceRepository : IInvoiceRepository
{
    private readonly PaymentDbContext _db;
    public InvoiceRepository(PaymentDbContext db) => _db = db;

    // ── Lookups ───────────────────────────────────────────────────────────────

    public Task<InvoiceHeaderEntity?> GetByIdAsync(long id, CancellationToken ct = default)
        => _db.InvoiceHeaders.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);

    public Task<InvoiceHeaderEntity?> GetByIdFullAsync(long id, CancellationToken ct = default)
        => _db.InvoiceHeaders
            .Include(x => x.Lines)
            .Include(x => x.TaxBreakdowns)
            .Include(x => x.StatusHistory)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);

    public Task<InvoiceHeaderEntity?> GetByInvoiceNumberAsync(
        string invoiceNumber, CancellationToken ct = default)
        => _db.InvoiceHeaders
            .FirstOrDefaultAsync(x => x.InvoiceNumber == invoiceNumber && !x.IsDeleted, ct);

    public Task<InvoiceHeaderEntity?> GetByTransactionIdAsync(
        long transactionId, CancellationToken ct = default)
        => _db.InvoiceHeaders
            .FirstOrDefaultAsync(x => x.PaymentTransactionId == transactionId && !x.IsDeleted, ct);

    // ── Paged lists ───────────────────────────────────────────────────────────

    public async Task<(List<InvoiceHeaderEntity> Items, int Total)> GetPagedAsync(
        InvoiceType?      type,
        InvoiceStatus?    status,
        string?           prefix,
        long?             buyerUserId,
        DateTime?         fromDate,
        DateTime?         toDate,
        string?           search,
        int               skip,
        int               take,
        CancellationToken ct = default)
    {
        var q = _db.InvoiceHeaders.Where(x => !x.IsDeleted).AsQueryable();

        if (type.HasValue)
            q = q.Where(x => x.InvoiceType == type.Value);
        if (status.HasValue)
            q = q.Where(x => x.Status == status.Value);
        if (buyerUserId.HasValue)
            q = q.Where(x => x.BuyerUserId == buyerUserId.Value);
        if (fromDate.HasValue)
            q = q.Where(x => x.IssueDateUtc >= fromDate.Value || x.CreateDate >= fromDate.Value);
        if (toDate.HasValue)
            q = q.Where(x => x.IssueDateUtc <= toDate.Value || x.CreateDate <= toDate.Value);
        if (!string.IsNullOrWhiteSpace(prefix))
            q = q.Where(x => x.InvoiceNumber != null && x.InvoiceNumber.StartsWith(prefix));
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x =>
                (x.InvoiceNumber != null && x.InvoiceNumber.Contains(search)) ||
                x.BuyerName.Contains(search));

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(x => x.CreateDate)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<(List<InvoiceHeaderEntity> Items, int Total)> GetByBuyerPagedAsync(
        long              buyerUserId,
        InvoiceStatus?    status,
        InvoiceType?      type,
        int               skip,
        int               take,
        CancellationToken ct = default)
    {
        var q = _db.InvoiceHeaders
            .Where(x => x.BuyerUserId == buyerUserId && !x.IsDeleted)
            .AsQueryable();

        if (status.HasValue) q = q.Where(x => x.Status == status.Value);
        if (type.HasValue)   q = q.Where(x => x.InvoiceType == type.Value);

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(x => x.IssueDateUtc ?? x.CreateDate)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        return (items, total);
    }

    public Task<List<InvoiceHeaderEntity>> GetCreditNotesByOriginalIdAsync(
        long originalInvoiceId, CancellationToken ct = default)
        => _db.InvoiceHeaders
            .Where(x => x.OriginalInvoiceId == originalInvoiceId && !x.IsDeleted)
            .OrderBy(x => x.CreateDate)
            .ToListAsync(ct);

    public Task<List<InvoiceHeaderEntity>> GetByTransactionIdAllAsync(
        long transactionId, CancellationToken ct = default)
        => _db.InvoiceHeaders
            .Where(x => x.PaymentTransactionId == transactionId && !x.IsDeleted)
            .OrderBy(x => x.CreateDate)
            .ToListAsync(ct);

    public Task<List<InvoiceHeaderEntity>> GetSentOverdueAsync(
        DateTime dueBefore, int batchSize, CancellationToken ct = default)
        => _db.InvoiceHeaders
            .Where(x =>
                x.Status == InvoiceStatus.Sent &&
                x.DueDateUtc.HasValue &&
                x.DueDateUtc.Value < dueBefore &&
                !x.IsDeleted)
            .OrderBy(x => x.DueDateUtc)
            .Take(batchSize)
            .ToListAsync(ct);

    public Task<List<InvoiceHeaderEntity>> GetSentSubscriptionOverdueAsync(
        DateTime dueBefore, CancellationToken ct = default)
        => _db.InvoiceHeaders
            .Where(x =>
                x.Status == InvoiceStatus.Sent &&
                x.InvoiceType == InvoiceType.SubscriptionInvoice &&
                x.DueDateUtc.HasValue &&
                x.DueDateUtc.Value < dueBefore &&
                !x.IsDeleted)
            .ToListAsync(ct);

    public Task<bool> ExistsForSourceAsync(
        InvoiceSourceType sourceType, long sourceId, CancellationToken ct = default)
        => _db.InvoiceHeaders
            .AnyAsync(x => x.SourceType == sourceType && x.SourceId == sourceId && !x.IsDeleted, ct);

    // ── Mutations ─────────────────────────────────────────────────────────────

    public Task AddAsync(InvoiceHeaderEntity invoice, CancellationToken ct = default)
        => _db.InvoiceHeaders.AddAsync(invoice, ct).AsTask();

    public void Update(InvoiceHeaderEntity invoice)
        => _db.InvoiceHeaders.Update(invoice);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
