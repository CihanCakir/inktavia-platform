using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.Transaction;
using Aizen.Modules.Payment.Repository.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Repository.Seed;

/// <summary>
/// Seeds TransactionRefundRecord mock data for refund history screens.
///
/// Targets:
///   TXN-20260601-0009  (SR 9010, Windlass repair, gross ₺29,000) — partial refund of ₺8,700
///   TXN-20260601-0008  (SR 9009, Water pressure, gross ₺18,000) — full refund (failed tx demo)
///
/// After seeding refund records, applies them to the parent transaction via
/// PaymentTransactionEntity.ApplyRefund() so TotalRefundedAmount and Status
/// are kept consistent.
///
/// Idempotent — skipped if any refund record already exists.
/// Must run AFTER PaymentTransactionMockSeed.
/// </summary>
public sealed class TransactionRefundMockSeed
{
    private readonly PaymentDbContext _db;
    private readonly ILogger<TransactionRefundMockSeed> _logger;

    public TransactionRefundMockSeed(PaymentDbContext db, ILogger<TransactionRefundMockSeed> logger)
    {
        _db     = db;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (await _db.TransactionRefunds.AnyAsync(ct))
        {
            _logger.LogDebug("TransactionRefund mock seed skipped — data already present.");
            return;
        }

        // ── Partial refund on TXN-20260601-0009 (SR 9010, Windlass) ─────────
        // Scenario: Work was partially completed; ₺8,700 returned to payer.
        // Transaction status after this: PartiallyRefunded
        // Net still owed to provider: 29,000 - 8,700 = 20,300 (commission adjusted separately)
        var tx9 = await _db.Transactions
            .FirstOrDefaultAsync(t => t.TransactionCode == "TXN-20260601-0009", ct);

        if (tx9 is not null)
        {
            var ref1 = TransactionRefundRecord.Create(
                paymentTransactionId:   tx9.Id,
                refundCode:             "REF-20260601-0001",
                amount:                 8_700m,
                currencyCode:           "TRY",
                refundType:             RefundType.Partial,
                reason:                 RefundReason.PartialServiceDelivered,
                adminNote:              "Windlass motor replaced but gearbox work not completed. Prorated refund for remaining scope.");
            ref1.MarkProcessed("IYZICO-REFUND-2024-0001", "Gateway confirmed partial refund within 3 business days.");

            await _db.TransactionRefunds.AddAsync(ref1, ct);
            await _db.SaveChangesAsync(ct); // save first so ref1.Id is assigned

            // Re-fetch to apply domain method (EF change tracker has it)
            tx9.ApplyRefund(ref1);
            // tx9 Status is now PartiallyRefunded, TotalRefundedAmount = 8,700
        }
        else
        {
            _logger.LogWarning("TransactionRefundMockSeed: TXN-20260601-0009 not found.");
        }

        // ── Second partial refund on same TXN-20260601-0009 ─────────────────
        // Scenario: Client disputed remaining scope — admin issues compensation credit.
        if (tx9 is not null)
        {
            var ref2 = TransactionRefundRecord.Create(
                paymentTransactionId:   tx9.Id,
                refundCode:             "REF-20260601-0002",
                amount:                 2_900m,
                currencyCode:           "TRY",
                refundType:             RefundType.Partial,
                reason:                 RefundReason.CompensationCredit,
                adminNote:              "Admin goodwill credit: 10% of gross for delayed service resolution.");
            ref2.MarkProcessed("IYZICO-REFUND-2024-0002");

            await _db.TransactionRefunds.AddAsync(ref2, ct);
            await _db.SaveChangesAsync(ct);

            tx9.ApplyRefund(ref2);
            // TotalRefundedAmount = 11,600 — still PartiallyRefunded (29,000 remaining = 17,400)
        }

        // ── Reversed refund on same TXN-20260601-0009 ─────────────────────
        // Scenario: REF-20260601-0002 was issued in error; reversed before bank settlement.
        // Demonstrates audit trail and reversal UI coverage.
        if (tx9 is not null)
        {
            var ref3 = TransactionRefundRecord.Create(
                paymentTransactionId:   tx9.Id,
                refundCode:             "REF-20260601-0003",
                amount:                 1_450m,
                currencyCode:           "TRY",
                refundType:             RefundType.Partial,
                reason:                 RefundReason.AdminForced,
                adminNote:              "Erroneously issued refund — reversed before bank settlement.");
            ref3.MarkProcessed("IYZICO-REFUND-2024-0003");
            // Immediately reverse it — demonstrates Reversed status in history
            tx9.ReverseRefund(ref3,
                reversalReason: "Issued in error; bank transfer had not settled yet.",
                adminNote:      "Reversed same business day — no customer impact.");

            await _db.TransactionRefunds.AddAsync(ref3, ct);
        }

        await _db.SaveChangesAsync(ct);

        var count = tx9 is null ? 0 : 3;
        _logger.LogInformation("TransactionRefund mock seed complete: {Count} refund records.", count);
    }
}
