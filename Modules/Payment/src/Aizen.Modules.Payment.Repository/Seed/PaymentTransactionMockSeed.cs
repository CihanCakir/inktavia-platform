using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Domain.Entities.Transaction;
using Aizen.Modules.Payment.Repository.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Repository.Seed;

/// <summary>
/// Seeds realistic PaymentTransaction mock data for admin panel QA and development.
///
/// Identity profile ID reference (from identity-profiles.json):
///   Participants (Payers)  : 11003 Ayşe, 11004 Mehmet, 11005 Deniz, 11006 Selin, 11007 Burak, 11008 Fatma
///   Providers  (Recipients): 11011 Marina Ops, 11012 Teknik Servis, 11013 CargoDry Ekip
///
/// SR IDs in use (from service-requests.json):
///   9001 Engine overhaul, 9002 Electrical, 9003 Navigation, 9004 Hull, 9005 Rigging
///   9006 Propulsion shaft, 9007 Upholstery, 9008 Safety, 9009 Water pressure, 9010 Windlass
///
/// Idempotent — skipped entirely if any transaction already exists.
/// </summary>
public sealed class PaymentTransactionMockSeed
{
    private readonly PaymentDbContext _db;
    private readonly ILogger<PaymentTransactionMockSeed> _logger;

    public PaymentTransactionMockSeed(PaymentDbContext db, ILogger<PaymentTransactionMockSeed> logger)
    {
        _db     = db;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (await _db.Transactions.AnyAsync(ct))
        {
            _logger.LogDebug("PaymentTransaction mock seed skipped — data already present.");
            return;
        }

        var transactions = BuildTransactions();
        await _db.Transactions.AddRangeAsync(transactions, ct);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("PaymentTransaction mock seed complete: {Count} transactions.", transactions.Count);
    }

    // ── Factory ───────────────────────────────────────────────────────────────

    private List<PaymentTransactionEntity> BuildTransactions()
    {
        var list = new List<PaymentTransactionEntity>();

        // ── SR 9001 — Engine Overhaul — Captured (in escrow) ────────────────
        // Payer: Ayşe (11003), Provider: Marina Ops (11011), STANDARD plan 12% comm
        var tx1 = MakeSrEscrow("TXN-20260601-0001", 9001, null, 11003, 11011,
            gross: 45_000m, commRate: 0.12m, currency: "TRY", idempotencyKey: "idm-sr-9001-escrow");
        tx1.Capture("IYZICO-CAPTURE-2024-0001");
        list.Add(tx1);

        // ── SR 9003 — Navigation Upgrade — Released (payout generated) ──────
        // Payer: Mehmet (11004), Provider: Teknik Servis (11012)
        var tx2 = MakeSrEscrow("TXN-20260601-0002", 9003, null, 11004, 11012,
            gross: 67_000m, commRate: 0.12m, currency: "TRY", idempotencyKey: "idm-sr-9003-escrow");
        tx2.Capture("IYZICO-CAPTURE-2024-0002");
        tx2.Release("SR completion approved by admin");
        list.Add(tx2);

        // ── SR 9004 — Hull Maintenance — Released ───────────────────────────
        // Payer: Deniz (11005), Provider: Marina Ops (11011)
        var tx3 = MakeSrEscrow("TXN-20260601-0003", 9004, null, 11005, 11011,
            gross: 15_000m, commRate: 0.12m, currency: "TRY", idempotencyKey: "idm-sr-9004-escrow");
        tx3.Capture("IYZICO-CAPTURE-2024-0003");
        tx3.Release("SR completion approved");
        list.Add(tx3);

        // ── SR 9005 — Rigging Inspection — Released ─────────────────────────
        // Payer: Selin (11006), Provider: Marina Ops (11011), FREE plan 18% comm
        var tx4 = MakeSrEscrow("TXN-20260601-0004", 9005, null, 11006, 11011,
            gross: 22_000m, commRate: 0.18m, currency: "TRY", idempotencyKey: "idm-sr-9005-escrow");
        tx4.Capture("IYZICO-CAPTURE-2024-0004");
        tx4.Release("SR completion approved");
        list.Add(tx4);

        // ── SR 9006 — Propulsion Shaft — Captured (still active) ────────────
        // Payer: Burak (11007), Provider: Teknik Servis (11012)
        var tx5 = MakeSrEscrow("TXN-20260601-0005", 9006, null, 11007, 11012,
            gross: 38_000m, commRate: 0.12m, currency: "TRY", idempotencyKey: "idm-sr-9006-escrow");
        tx5.Capture("IYZICO-CAPTURE-2024-0005");
        list.Add(tx5);

        // ── SR 9007 — Upholstery — PendingIntent (offer accepted, not paid) ─
        // Payer: Fatma (11008), Provider: Marina Ops (11011)
        var tx6 = MakeSrEscrow("TXN-20260601-0006", 9007, null, 11008, 11011,
            gross: 12_500m, commRate: 0.12m, currency: "TRY", idempotencyKey: "idm-sr-9007-escrow");
        // stays PendingIntent
        list.Add(tx6);

        // ── SR 9008 — Safety Inspection — Released (small job done) ─────────
        // Payer: Ayşe (11003), Provider: Marina Ops (11011)
        var tx7 = MakeSrEscrow("TXN-20260601-0007", 9008, null, 11003, 11011,
            gross: 8_500m, commRate: 0.12m, currency: "TRY", idempotencyKey: "idm-sr-9008-escrow");
        tx7.Capture("IYZICO-CAPTURE-2024-0007");
        tx7.Release("SR completion approved");
        list.Add(tx7);

        // ── SR 9009 — Water Pressure Fault — Failed (gateway rejection) ─────
        // Payer: Mehmet (11004), Provider: Teknik Servis (11012)
        var tx8 = MakeSrEscrow("TXN-20260601-0008", 9009, null, 11004, 11012,
            gross: 18_000m, commRate: 0.12m, currency: "TRY", idempotencyKey: "idm-sr-9009-escrow");
        tx8.MarkFailed();
        list.Add(tx8);

        // ── SR 9010 — Windlass Repair — Captured (partial refund applied) ───
        // Payer: Deniz (11005), Provider: CargoDry Ekip (11013)
        var tx9 = MakeSrEscrow("TXN-20260601-0009", 9010, null, 11005, 11013,
            gross: 29_000m, commRate: 0.12m, currency: "TRY", idempotencyKey: "idm-sr-9010-escrow");
        tx9.Capture("IYZICO-CAPTURE-2024-0009");
        // partial refund applied by TransactionRefundMockSeed after this seeder runs
        list.Add(tx9);

        // ── SR 9002 — Electrical Panel — Cancelled (intent voided) ──────────
        // Payer: Selin (11006), Provider: Teknik Servis (11012)
        var tx10 = MakeSrEscrow("TXN-20260601-0010", 9002, null, 11006, 11012,
            gross: 28_500m, commRate: 0.12m, currency: "TRY", idempotencyKey: "idm-sr-9002-escrow");
        tx10.Cancel(CancellationReason.PayerRequest, "Client cancelled before payment cleared.");
        list.Add(tx10);

        // ── SR 9001 — CargoDry Demo SR — Released (CargoDry Ekip) ───────────
        // Payer: Burak (11007), Provider: CargoDry Ekip (11013)
        var tx11 = MakeSrEscrow("TXN-20260601-0011", 30005, null, 11007, 11013,
            gross: 9_500m, commRate: 0.05m, currency: "TRY", idempotencyKey: "idm-sr-30005-escrow");
        tx11.Capture("IYZICO-CAPTURE-2024-0011");
        tx11.Release("SR completion approved — CargoDry demo");
        list.Add(tx11);

        // ── Subscription — Provider Standard (11011) — Captured ─────────────
        var tx12 = Make(
            code:          "TXN-20260601-0012",
            type:          TransactionType.ProviderPlanSubscription,
            contextType:   TransactionContextType.Subscription,
            contextId:     1,   // ProviderPlanId=1 (STANDARD)
            contextSubId:  null,
            payerId:       11011,
            recipientId:   null,
            gross:         499m,
            commRate:      0m,   // platform keeps entire sub fee — no commission
            currency:      "TRY",
            gateway:       "IYZICO",
            idempotency:   "idm-sub-provider-11011-2026-06",
            escrow:        false);
        tx12.Capture("IYZICO-SUB-CAPTURE-11011-01");
        list.Add(tx12);

        // ── Subscription — Provider Premium (11012) — Captured ───────────────
        var tx13 = Make(
            code:          "TXN-20260601-0013",
            type:          TransactionType.ProviderPlanSubscription,
            contextType:   TransactionContextType.Subscription,
            contextId:     3,   // ProviderPlanId=3 (PREMIUM_PARTNER)
            contextSubId:  null,
            payerId:       11012,
            recipientId:   null,
            gross:         999m,
            commRate:      0m,
            currency:      "TRY",
            gateway:       "IYZICO",
            idempotency:   "idm-sub-provider-11012-2026-06",
            escrow:        false);
        tx13.Capture("IYZICO-SUB-CAPTURE-11012-01");
        list.Add(tx13);

        // ── Subscription — Participant Gold (11003) — Captured ───────────────
        var tx14 = Make(
            code:          "TXN-20260601-0014",
            type:          TransactionType.ParticipantSubscription,
            contextType:   TransactionContextType.Subscription,
            contextId:     2,   // ParticipantPlanId=2 (GOLD)
            contextSubId:  null,
            payerId:       11003,
            recipientId:   null,
            gross:         199m,
            commRate:      0m,
            currency:      "TRY",
            gateway:       "IYZICO",
            idempotency:   "idm-sub-participant-11003-2026-06",
            escrow:        false);
        tx14.Capture("IYZICO-SUB-CAPTURE-11003-01");
        list.Add(tx14);

        // ── Subscription — Participant Platinum (11004) — Captured ───────────
        var tx15 = Make(
            code:          "TXN-20260601-0015",
            type:          TransactionType.ParticipantSubscription,
            contextType:   TransactionContextType.Subscription,
            contextId:     3,   // ParticipantPlanId=3 (PLATINUM)
            contextSubId:  null,
            payerId:       11004,
            recipientId:   null,
            gross:         399m,
            commRate:      0m,
            currency:      "TRY",
            gateway:       "IYZICO",
            idempotency:   "idm-sub-participant-11004-2026-06",
            escrow:        false);
        tx15.Capture("IYZICO-SUB-CAPTURE-11004-01");
        list.Add(tx15);

        // ── CargoDry Renewal — Kit CDK-STAN-0003 — Captured ─────────────────
        var tx16 = Make(
            code:          "TXN-20260601-0016",
            type:          TransactionType.CargoDryRenewal,
            contextType:   TransactionContextType.CargoDry,
            contextId:     3,   // Kit id=3 (CDK-STAN-0003, userId 10003, vessel 2)
            contextSubId:  null,
            payerId:       10003,
            recipientId:   null,
            gross:         350m,
            commRate:      0.05m,
            currency:      "TRY",
            gateway:       "IYZICO",
            idempotency:   "idm-cargodry-renew-kit3-2026-06",
            escrow:        false);
        tx16.Capture("IYZICO-CDR-CAPTURE-KIT3-01");
        list.Add(tx16);

        return list;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static PaymentTransactionEntity MakeSrEscrow(
        string code, long srId, long? offerId,
        long payerId, long providerId,
        decimal gross, decimal commRate, string currency,
        string idempotencyKey) =>
        Make(
            code:         code,
            type:         TransactionType.ServiceRequestEscrow,
            contextType:  TransactionContextType.ServiceRequest,
            contextId:    srId,
            contextSubId: offerId,
            payerId:      payerId,
            recipientId:  providerId,
            gross:        gross,
            commRate:     commRate,
            currency:     currency,
            gateway:      "IYZICO",
            idempotency:  idempotencyKey,
            escrow:       true);

    private static PaymentTransactionEntity Make(
        string code, TransactionType type,
        TransactionContextType contextType, long contextId, long? contextSubId,
        long payerId, long? recipientId,
        decimal gross, decimal commRate, string currency,
        string gateway, string idempotency, bool escrow)
    {
        var vatRate    = 0.20m;
        var commission = Math.Round(gross * commRate, 2);
        var vat        = Math.Round(commission * vatRate, 2);
        var net        = gross - commission - vat;

        return PaymentTransactionEntity.Create(
            transactionCode:        code,
            transactionType:        type,
            contextType:            contextType,
            contextId:              contextId,
            contextSubId:           contextSubId,
            payerProfileId:         payerId,
            recipientProfileId:     recipientId,
            grossAmount:            gross,
            commissionAmount:       commission,
            commissionRateSnapshot: commRate,
            vatOnCommission:        vat,
            netPayoutAmount:        net,
            discountAmount:         0m,
            currencyCode:           currency,
            gatewayProvider:        gateway,
            idempotencyKey:         idempotency,
            escrowRequired:         escrow);
    }
}
