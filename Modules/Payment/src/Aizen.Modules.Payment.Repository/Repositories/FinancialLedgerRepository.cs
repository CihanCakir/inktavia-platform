using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.Reporting;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Aizen.Modules.Payment.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Payment.Repository.Repositories;

/// <summary>BE-P12 §15 — append-only financial ledger persistence + reporting reads.</summary>
public sealed class FinancialLedgerRepository : IFinancialLedgerRepository
{
    private readonly PaymentDbContext _db;
    public FinancialLedgerRepository(PaymentDbContext db) => _db = db;

    public Task<bool> ExistsAsync(
        LedgerSourceType sourceType, long sourceRef, LedgerAccountLine accountLine, bool isReversal, CancellationToken ct = default)
        => _db.FinancialLedgerEntries.AnyAsync(x =>
            x.SourceType == sourceType && x.SourceRef == sourceRef && x.AccountLine == accountLine && x.IsReversal == isReversal, ct);

    public Task AddAsync(FinancialLedgerEntryEntity entry, CancellationToken ct = default)
        => _db.FinancialLedgerEntries.AddAsync(entry, ct).AsTask();

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);

    public Task<List<FinancialLedgerEntryEntity>> GetForPeriodAsync(
        DateTime fromUtc, DateTime toUtc, string? currency, CancellationToken ct = default)
    {
        var q = _db.FinancialLedgerEntries.AsNoTracking()
            .Where(x => x.OccurredAtUtc >= fromUtc && x.OccurredAtUtc < toUtc);
        if (!string.IsNullOrWhiteSpace(currency))
        {
            var cur = currency.ToUpperInvariant();
            q = q.Where(x => x.CurrencyCode == cur);
        }
        return q.ToListAsync(ct);
    }

    public async Task<(List<FinancialLedgerEntryEntity> Items, int Total)> GetPagedAsync(
        LedgerAccountLine? accountLine, LedgerSourceType? sourceType, long? providerProfileId,
        DateTime? fromUtc, DateTime? toUtc, string? currency, int skip, int take, CancellationToken ct = default)
    {
        var q = _db.FinancialLedgerEntries.AsNoTracking().AsQueryable();

        if (accountLine.HasValue)       q = q.Where(x => x.AccountLine == accountLine.Value);
        if (sourceType.HasValue)        q = q.Where(x => x.SourceType == sourceType.Value);
        if (providerProfileId.HasValue) q = q.Where(x => x.ProviderProfileId == providerProfileId.Value);
        if (fromUtc.HasValue)           q = q.Where(x => x.OccurredAtUtc >= fromUtc.Value);
        if (toUtc.HasValue)             q = q.Where(x => x.OccurredAtUtc < toUtc.Value);
        if (!string.IsNullOrWhiteSpace(currency))
        {
            var cur = currency.ToUpperInvariant();
            q = q.Where(x => x.CurrencyCode == cur);
        }

        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(x => x.OccurredAtUtc).ThenBy(x => x.Id)
            .Skip(skip).Take(take).ToListAsync(ct);
        return (items, total);
    }

    public Task<List<FinancialLedgerEntryEntity>> GetBySourceAsync(
        LedgerSourceType sourceType, long sourceRef, CancellationToken ct = default)
        => _db.FinancialLedgerEntries.AsNoTracking()
            .Where(x => x.SourceType == sourceType && x.SourceRef == sourceRef)
            .ToListAsync(ct);
}
