using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Repository.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Application.Services;

/// <summary>
/// BE-P12 §15 — one-off <b>idempotent backfill</b> of the financial ledger from the existing immutable sources
/// (snapshots / refund allocations / chargebacks / premium purchases / subscriptions), so historical data reports
/// correctly. Append-only and safe to re-run — every line goes through the posting service's
/// <c>(SourceType, SourceRef, AccountLine, IsReversal)</c> idempotency guard.
/// </summary>
public sealed class FinancialLedgerBackfillService
{
    private readonly PaymentDbContext                _db;
    private readonly FinancialLedgerPostingService   _posting;
    private readonly ILogger<FinancialLedgerBackfillService> _logger;

    public FinancialLedgerBackfillService(
        PaymentDbContext db, FinancialLedgerPostingService posting, ILogger<FinancialLedgerBackfillService> logger)
    {
        _db      = db;
        _posting = posting;
        _logger  = logger;
    }

    public async Task<int> BackfillAsync(CancellationToken ct = default)
    {
        var before = await _db.FinancialLedgerEntries.CountAsync(ct);

        // ── Acceptance snapshots → revenue/liability/discount/contribution (join the tx for provider/customer ids). ──
        var snapshots = await _db.PaymentEconomicsSnapshots.AsNoTracking().ToListAsync(ct);
        foreach (var s in snapshots)
        {
            var tx = await _db.Transactions.AsNoTracking().FirstOrDefaultAsync(t => t.EconomicsSnapshotId == s.Id, ct);
            if (tx is null) continue;
            await _posting.PostAcceptanceAsync(tx, s, ct);
        }

        // ── Refund allocations → reversal + refund/recovery/advance (join the record for the tx). ──
        var allocations = await _db.RefundAllocations.AsNoTracking().ToListAsync(ct);
        foreach (var a in allocations)
        {
            var record = await _db.Set<Domain.Entities.Transaction.TransactionRefundRecord>()
                .AsNoTracking().FirstOrDefaultAsync(r => r.Id == a.RefundRecordId, ct);
            if (record is null) continue;
            var tx = await _db.Transactions.AsNoTracking().FirstOrDefaultAsync(t => t.Id == record.PaymentTransactionId, ct);
            if (tx is null) continue;
            await _posting.PostRefundAsync(tx, record.Id, a, ct);
        }

        // ── Chargebacks. ──
        var chargebacks = await _db.ChargebackRecords.AsNoTracking().ToListAsync(ct);
        foreach (var c in chargebacks)
        {
            var tx = await _db.Transactions.AsNoTracking().FirstOrDefaultAsync(t => t.Id == c.PaymentTransactionId, ct);
            await _posting.PostChargebackAsync(c, tx?.RecipientProfileId, ct);
        }

        // ── Premium purchases: Paid → revenue; Refunded → revenue then its reversal (SourceRef = purchase id backfill key). ──
        var purchases = await _db.PremiumPurchases.AsNoTracking()
            .Where(p => p.Status == PremiumPurchaseStatus.Paid || p.Status == PremiumPurchaseStatus.Refunded)
            .ToListAsync(ct);
        foreach (var p in purchases)
        {
            await _posting.PostPremiumPaidAsync(p, p.PaymentTransactionId, ct);
            if (p.Status == PremiumPurchaseStatus.Refunded)
                await _posting.PostPremiumRefundAsync(p, p.PaymentTransactionId, ct);   // keyed by the purchase → idempotent with runtime
        }

        // ── Provider + participant subscriptions with a positive paid amount. ──
        var providerSubs = await _db.ProviderSubscriptions.AsNoTracking().Where(x => x.PaidAmount > 0m).ToListAsync(ct);
        foreach (var s in providerSubs)
            await _posting.PostSubscriptionAsync(s.Id, s.PaidAmount, s.CurrencyCode, true, s.ProviderProfileId,
                s.PaymentTransactionId, s.CreateDate ?? DateTime.UtcNow, ct);

        var participantSubs = await _db.ParticipantSubscriptions.AsNoTracking().Where(x => x.PaidAmount > 0m).ToListAsync(ct);
        foreach (var s in participantSubs)
            await _posting.PostSubscriptionAsync(s.Id, s.PaidAmount, s.CurrencyCode, false, s.ParticipantProfileId,
                s.PaymentTransactionId, s.CreateDate ?? DateTime.UtcNow, ct);

        await _db.SaveChangesAsync(ct);
        var added = await _db.FinancialLedgerEntries.CountAsync(ct) - before;
        _logger.LogInformation("Financial ledger backfill complete: {Added} new entries ({Before} → {After}).",
            added, before, before + added);
        return added;
    }
}
