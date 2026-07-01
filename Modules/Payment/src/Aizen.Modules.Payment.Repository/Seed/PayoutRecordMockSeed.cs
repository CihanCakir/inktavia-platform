using Aizen.Modules.Payment.Domain.Entities.Payout;
using Aizen.Modules.Payment.Repository.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace Aizen.Modules.Payment.Repository.Seed;

/// <summary>
/// Seeds PayoutRecord mock data for admin payout management screens.
///
/// Payouts are only generated for Released transactions. Transaction DB IDs are
/// resolved at seed time by TransactionCode lookup so this seeder must run
/// AFTER PaymentTransactionMockSeed.
///
/// Coverage:
///   PAYOUT-001  TX TXN-20260601-0002 (SR 9003, Provider 11012)  → Completed
///   PAYOUT-002  TX TXN-20260601-0003 (SR 9004, Provider 11011)  → Completed
///   PAYOUT-003  TX TXN-20260601-0004 (SR 9005, Provider 11011)  → Processing
///   PAYOUT-004  TX TXN-20260601-0007 (SR 9008, Provider 11011)  → OnHold
///   PAYOUT-005  TX TXN-20260601-0011 (SR 30005, Provider 11013) → Pending
///   PAYOUT-006  TX TXN-20260601-0001 (SR 9001, Provider 11011)  → Failed (manual sim)
///
/// Idempotent — skipped if any payout record already exists.
/// </summary>
public sealed class PayoutRecordMockSeed
{
    private readonly PaymentDbContext _db;
    private readonly ILogger<PayoutRecordMockSeed> _logger;

    public PayoutRecordMockSeed(PaymentDbContext db, ILogger<PayoutRecordMockSeed> logger)
    {
        _db     = db;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (await _db.PayoutRecords.AnyAsync(ct))
        {
            _logger.LogDebug("PayoutRecord mock seed skipped — data already present.");
            return;
        }

        var records = new List<PayoutRecordEntity>();

        // ── PAYOUT-001: SR 9003, Provider 11012, ₺57,352 → Completed ─────────
        // Released tx: gross 67,000 − comm 8,040 − VAT 1,608 = 57,352
        var tx2Id = await ResolveTransactionIdAsync("TXN-20260601-0002", ct);
        if (tx2Id > 0)
        {
            var p1 = PayoutRecordEntity.Create(11012, tx2Id, 57_352m, "TRY", "IYZICO");
            p1.MarkCompleted("IYZICO-PAYOUT-2024-0001", "Automatically processed via Iyzico marketplace transfer.");
            records.Add(p1);
        }

        // ── PAYOUT-002: SR 9004, Provider 11011, ₺12,840 → Completed ─────────
        // Released tx: gross 15,000 − comm 1,800 − VAT 360 = 12,840
        var tx3Id = await ResolveTransactionIdAsync("TXN-20260601-0003", ct);
        if (tx3Id > 0)
        {
            var p2 = PayoutRecordEntity.Create(11011, tx3Id, 12_840m, "TRY", "IYZICO");
            p2.MarkCompleted("IYZICO-PAYOUT-2024-0002", "Automatically processed via Iyzico marketplace transfer.");
            records.Add(p2);
        }

        // ── PAYOUT-003: SR 9005, Provider 11011, ₺14,828 → Processing ─────────
        // Released tx: gross 22,000 − comm 3,960 (18%) − VAT 792 = 17,248
        // Note: 18% FREE plan rate → net = 22000 − 3960 − 792 = 17,248
        var tx4Id = await ResolveTransactionIdAsync("TXN-20260601-0004", ct);
        if (tx4Id > 0)
        {
            var p3 = PayoutRecordEntity.Create(11011, tx4Id, 17_248m, "TRY", "IYZICO");
            p3.MarkProcessing();
            records.Add(p3);
        }

        // ── PAYOUT-004: SR 9008, Provider 11011, ₺7,276 → OnHold ────────────
        // Released tx: gross 8,500 − comm 1,020 − VAT 204 = 7,276
        var tx7Id = await ResolveTransactionIdAsync("TXN-20260601-0007", ct);
        if (tx7Id > 0)
        {
            var p4 = PayoutRecordEntity.Create(11011, tx7Id, 7_276m, "TRY", "IYZICO");
            p4.Hold("AML compliance review — transaction value threshold exceeded.", "Flagged by auto-compliance job.");
            records.Add(p4);
        }

        // ── PAYOUT-005: SR 30005 CargoDry, Provider 11013, ₺9,025 → Pending ──
        // Released tx: gross 9,500 − comm 475 (5%) − VAT 95 = 8,930
        var tx11Id = await ResolveTransactionIdAsync("TXN-20260601-0011", ct);
        if (tx11Id > 0)
        {
            var p5 = PayoutRecordEntity.Create(11013, tx11Id, 8_930m, "TRY", "IYZICO");
            // stays Pending — awaiting batch processing window
            records.Add(p5);
        }

        // ── PAYOUT-006: SR 9001, Provider 11011 → Failed ─────────────────────
        // TX1 is Captured (not released), but seeding a Failed payout for UI test coverage.
        // In production, payouts are created only on Release. This is a demo-only record.
        var tx1Id = await ResolveTransactionIdAsync("TXN-20260601-0001", ct);
        if (tx1Id > 0)
        {
            var p6 = PayoutRecordEntity.Create(11011, tx1Id, 38_520m, "TRY", "IYZICO");
            p6.MarkFailed("Iyzico sub-merchant account not verified — transfer rejected by gateway.");
            records.Add(p6);
        }

        if (records.Count > 0)
        {
            await _db.PayoutRecords.AddRangeAsync(records, ct);
            await _db.SaveChangesAsync(ct);
        }

        _logger.LogInformation("PayoutRecord mock seed complete: {Count} records.", records.Count);
    }

    private async Task<long> ResolveTransactionIdAsync(string transactionCode, CancellationToken ct)
    {
        var id = await _db.Transactions
            .Where(t => t.TransactionCode == transactionCode)
            .Select(t => t.Id)
            .FirstOrDefaultAsync(ct);

        if (id == 0)
            _logger.LogWarning("PayoutMockSeed: transaction '{Code}' not found — payout skipped.", transactionCode);

        return id;
    }
}
