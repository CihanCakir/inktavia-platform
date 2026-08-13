using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.Reporting;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Aizen.Modules.Payment.Domain.Money;
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

    public async Task<IReadOnlyList<MonthlyRevenueCommissionPoint>> GetMonthlyRevenueCommissionAsync(
        int months, CancellationToken ct = default)
    {
        if (months <= 0) months = 12;

        // UTC calendar-month buckets: start = first day of the month (months-1) before the current month.
        var nowUtc = DateTime.UtcNow;
        var currentMonthStartUtc = new DateTime(nowUtc.Year, nowUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var startUtc = currentMonthStartUtc.AddMonths(-(months - 1));

        // Bounded fetch of the window, then bucket IN MEMORY (avoids the Npgsql timestamptz GroupBy 500).
        // Settlement currency only (TRY) — summing across currencies would be meaningless for the chart.
        var rows = await _db.FinancialLedgerEntries.AsNoTracking()
            .Where(x => x.OccurredAtUtc >= startUtc && x.CurrencyCode == "TRY")
            .Select(x => new { x.OccurredAtUtc, x.Nature, x.AccountLine, x.Amount, x.IsReversal })
            .ToListAsync(ct);

        // Pre-seed the N zero-filled buckets oldest→newest, so months with no activity still render.
        var buckets = new List<MonthlyRevenueCommissionPoint>(months);
        var byMonth = new Dictionary<DateOnly, MonthlyRevenueCommissionPoint>(months);
        for (var i = 0; i < months; i++)
        {
            var m = startUtc.AddMonths(i);
            var point = new MonthlyRevenueCommissionPoint { Month = new DateOnly(m.Year, m.Month, 1) };
            buckets.Add(point);
            byMonth[point.Month] = point;
        }

        foreach (var r in rows)
        {
            var key = new DateOnly(r.OccurredAtUtc.Year, r.OccurredAtUtc.Month, 1);
            if (!byMonth.TryGetValue(key, out var bucket))
                continue; // defensive: outside the seeded window

            // Reuse the ledger's sign convention (Amount ≥ 0; IsReversal flips it) — do NOT re-derive signs.
            var signed = r.IsReversal ? -r.Amount : r.Amount;
            if (r.Nature == LedgerEntryNature.Revenue)
                bucket.Revenue += signed;
            // Commission is a subset of revenue: the provider-commission line specifically.
            if (r.AccountLine == LedgerAccountLine.ProviderCommissionRevenue)
                bucket.Commission += signed;
        }

        foreach (var b in buckets)
        {
            b.Revenue = MoneyMath.Round(b.Revenue);
            b.Commission = MoneyMath.Round(b.Commission);
        }

        return buckets;
    }
}
