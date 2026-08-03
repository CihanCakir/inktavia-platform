using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.Economics;
using Aizen.Modules.Payment.Domain.Entities.RefundAllocation;
using Aizen.Modules.Payment.Domain.Entities.Transaction;
using Aizen.Modules.Payment.Repository.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Repository.Seed;

/// <summary>
/// SEED_WAVE_B — lights up the two data-less <b>positive</b> Wave B branches on-screen for "PROVIDER 2 AS"
/// (RecipientProfileId 100011): Part A economics breakdown ("Kazanç kırılımı") and Part C (İtirazlı chip +
/// refund breakdown + over-limit negative-balance strip). It only adds DATA — no product/FE changes.
///
/// <para><b>Domain factories, NOT raw INSERTs</b> — every record goes through its guarded construction path so the
/// invariants hold: the immutable <see cref="PaymentEconomicsSnapshotEntity.CreateFromLines"/> (the 8 §20.15
/// equalities, zero tolerance); the pure <see cref="RefundAllocationCalculator"/> for the 9-amount refund breakdown;
/// and <see cref="ProviderBalanceEntity"/> domain methods (<c>Clawback</c>) for the negative-balance ledger. The
/// disputed/refunded record follows the real state machine (Release → Dispute → ResolveDispute → ApplyRefund) so
/// <c>DisputedAt</c> survives while the tx is still refundable.</para>
///
/// <para>Idempotent (find-or-create on the demo tx codes) and demo-labelled. No migration. Runs from the payment-api
/// dev/local startup after <see cref="TransactionRefundMockSeed"/>. To remove the demo, delete this seeder + its wiring
/// (and, on an existing DB, the two <c>TXN-20260610-P204/P205</c> transactions and their linked snapshot / refund /
/// allocation / provider-balance rows).</para>
/// </summary>
public sealed class Provider2PositiveBranchMockSeed
{
    private const long   Provider2   = 100011;   // "PROVIDER 2 AS" (the verification provider)
    private const string CurrencyTry = "TRY";

    // Demo transaction codes — idempotency keys for this seeder.
    private const string TxEconomicsCode = "TXN-20260610-P204";   // Part A — Released + economics snapshot
    private const string TxDisputedCode  = "TXN-20260610-P205";   // Part C — disputed/refunded + negative balance

    private readonly PaymentDbContext _db;
    private readonly ILogger<Provider2PositiveBranchMockSeed> _logger;

    public Provider2PositiveBranchMockSeed(PaymentDbContext db, ILogger<Provider2PositiveBranchMockSeed> logger)
    {
        _db     = db;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await SeedEconomicsBreakdownAsync(ct);   // Part A
        await SeedDisputedRefundAsync(ct);       // Part C
    }

    // ── Part A — a Released tx WITH an economics snapshot → "Kazanç kırılımı" renders ───────────────────────
    private async Task SeedEconomicsBreakdownAsync(CancellationToken ct)
    {
        if (await _db.Transactions.AnyAsync(t => t.TransactionCode == TxEconomicsCode, ct))
        {
            _logger.LogDebug("Provider2 Wave B economics seed skipped — {Code} already present.", TxEconomicsCode);
            return;
        }

        // Balanced snapshot: service 19,000 @ 12% → provider net 16,720; platform fee 5% = 950 net + 190 VAT = 1,140 gross;
        // customer total 20,140; platform gross share 3,420 (= commission 2,280 + fee gross 1,140). All 8 equalities hold.
        var serviceLine = new LineEconomicsInput(
            LineRef: "L-P204-SVC", ItemType: 1, PricingMethod: 1,
            GrossBeforeDiscount: 19_000m, CustomerDiscount: 0m, ProviderFundedDiscount: 0m, PlatformFundedDiscount: 0m,
            CommissionEligibility: LineCommissionEligibility.Eligible,
            CommissionBase: 19_000m, CommissionRate: 0.12m, CommissionAmount: 2_280m,
            ProviderNet: 16_720m, LineVat: 0m, LineTotal: 19_000m,
            RuleId: 7, RuleCode: "PLAN-STD", Commissionable: true, SortOrder: 0);

        var platformFee = new PlatformFeeInput(
            RuleId: 3, Rate: 0.05m, Minimum: 99m, Maximum: 1_500m,
            Base: 19_000m, Net: 950m, Vat: 190m, Gross: 1_140m);

        var snapshot = PaymentEconomicsSnapshotEntity.CreateFromLines(
            contextId: 20_004, currencyCode: CurrencyTry,
            lines: new[] { serviceLine },
            platformFee: platformFee,
            customerPayableServiceAmount: 19_000m,
            contextType: TransactionContextType.ServiceRequest);

        await _db.PaymentEconomicsSnapshots.AddAsync(snapshot, ct);
        await _db.SaveChangesAsync(ct);   // materialise snapshot.Id for the link

        // A clean Released service escrow tx for Provider 2, linked to the snapshot (customer total mirrors the snapshot).
        var tx = MakeSrEscrow(TxEconomicsCode, srId: 20_004, payerId: 11_006, providerId: Provider2,
            gross: 20_140m, commRate: 0.12m, idempotencyKey: "idm-sr-20004-p2-econ-demo");
        tx.Capture("IYZICO-CAPTURE-P2-0204");
        tx.Release("SR completion approved — provider2 (Wave B economics demo)");
        tx.LinkEconomicsSnapshot(snapshot.Id);

        await _db.Transactions.AddAsync(tx, ct);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Provider2 Wave B economics seed complete: {Code} → snapshot {Snap} (customerTotal {Total}, providerNet {Net}).",
            TxEconomicsCode, snapshot.SnapshotCode, snapshot.CustomerTotalAmountSnapshot, snapshot.ProviderNetAmountSnapshot);
    }

    // ── Part C — disputed + release-after refund + over-limit negative balance → İtirazlı + refund + strip ────
    private async Task SeedDisputedRefundAsync(CancellationToken ct)
    {
        if (await _db.Transactions.AnyAsync(t => t.TransactionCode == TxDisputedCode, ct))
        {
            _logger.LogDebug("Provider2 Wave B disputed/refund seed skipped — {Code} already present.", TxDisputedCode);
            return;
        }

        // Balanced snapshot: service 12,000 @ 12% → provider net 10,560; platform fee 5% = 600 net + 120 VAT = 720 gross;
        // customer total 12,720.
        var serviceLine = new LineEconomicsInput(
            LineRef: "L-P205-SVC", ItemType: 1, PricingMethod: 1,
            GrossBeforeDiscount: 12_000m, CustomerDiscount: 0m, ProviderFundedDiscount: 0m, PlatformFundedDiscount: 0m,
            CommissionEligibility: LineCommissionEligibility.Eligible,
            CommissionBase: 12_000m, CommissionRate: 0.12m, CommissionAmount: 1_440m,
            ProviderNet: 10_560m, LineVat: 0m, LineTotal: 12_000m,
            RuleId: 7, RuleCode: "PLAN-STD", Commissionable: true, SortOrder: 0);

        var platformFee = new PlatformFeeInput(
            RuleId: 3, Rate: 0.05m, Minimum: 99m, Maximum: 1_500m,
            Base: 12_000m, Net: 600m, Vat: 120m, Gross: 720m);

        var snapshot = PaymentEconomicsSnapshotEntity.CreateFromLines(
            contextId: 20_005, currencyCode: CurrencyTry,
            lines: new[] { serviceLine },
            platformFee: platformFee,
            customerPayableServiceAmount: 12_000m,
            contextType: TransactionContextType.ServiceRequest);

        await _db.PaymentEconomicsSnapshots.AddAsync(snapshot, ct);
        await _db.SaveChangesAsync(ct);

        // Released service escrow tx (customer total 12,720), then run the real dispute/refund state machine:
        //   Dispute() sets DisputedAt → ResolveDispute() returns it to Released (DisputedAt survives) → ApplyRefund().
        var tx = MakeSrEscrow(TxDisputedCode, srId: 20_005, payerId: 11_007, providerId: Provider2,
            gross: 12_720m, commRate: 0.12m, idempotencyKey: "idm-sr-20005-p2-dispute-demo");
        tx.Capture("IYZICO-CAPTURE-P2-0205");
        tx.Release("SR completion approved — provider2 (Wave B dispute demo)");
        tx.LinkEconomicsSnapshot(snapshot.Id);
        tx.Dispute();                                                             // → DisputedAt set
        tx.ResolveDispute("İtiraz müşteri lehine sonuçlandı — tam iade uygulanıyor.");  // → Released, DisputedAt kept

        await _db.Transactions.AddAsync(tx, ct);
        await _db.SaveChangesAsync(ct);   // materialise tx.Id for the refund record

        // Full gateway refund (customer total 12,720). Processed → applied to the tx.
        var refund = TransactionRefundRecord.Create(
            paymentTransactionId: tx.Id,
            refundCode:           "REF-20260610-P205",
            amount:               12_720m,
            currencyCode:         CurrencyTry,
            refundType:           RefundType.Full,
            reason:               RefundReason.DisputeResolvedForPayer,
            adminNote:            "Wave B demo — dispute resolved for payer; full release-after refund.");
        refund.MarkProcessed("IYZICO-REFUND-P2-0205", "Gateway confirmed full refund.");

        await _db.TransactionRefunds.AddAsync(refund, ct);
        await _db.SaveChangesAsync(ct);   // materialise refund.Id

        tx.ApplyRefund(refund);           // TotalRefundedAmount = 12,720 → status Refunded (DisputedAt untouched)

        // §7.5 refund allocation from the immutable snapshot — full refund → service 12,000 (provider net 10,560 +
        // commission 1,440); platform fee refunded in Full. Release-after → provider recovery 10,560.
        const RefundCause cause = RefundCause.DisputeCustomerFavoured;
        var allocation = RefundAllocationCalculator.Resolve(
            snapshot,
            refundServiceAmount:   snapshot.ServiceAmountSnapshot,   // 12,000 (full)
            cause:                 cause,
            releaseState:          ReleaseState.AfterProviderRelease,
            platformFeeRefundMode: PlatformFeeRefundMode.Full);

        // §7.3 release-after recovery: the platform advances the provider's net reversal to the customer immediately,
        // then claws it back into the provider's negative-balance ledger (mirrors RefundAllocationService).
        var balance = await GetOrCreateOverLimitBalanceAsync(ct);
        balance.Clawback(
            allocation.ProviderNetReversalAmount,                    // 10,560
            ProviderBalanceMovementType.RefundClawback,
            refundRecordId: refund.Id, chargebackRecordId: null,
            note: $"Refund {refund.RefundCode} (Wave B demo)", DateTime.UtcNow);
        _db.ProviderBalances.Update(balance);

        allocation = allocation with
        {
            PlatformAdvancedRefundAmount     = allocation.ProviderNetReversalAmount,   // 10,560 advanced
            RemainingProviderNegativeBalance = balance.NegativeAmount,                 // 10,560 owed
        };

        var allocationEntity = RefundAllocationEntity.Create(
            refund.Id, snapshot.Id, cause, ReleaseState.AfterProviderRelease, CurrencyTry, allocation);
        await _db.RefundAllocations.AddAsync(allocationEntity, ct);
        await _db.SaveChangesAsync(ct);   // materialise allocationEntity.Id + persist the clawback movement

        refund.SetAllocation(cause, ReleaseState.AfterProviderRelease, allocationEntity.Id);  // link → RefundSummary read-model
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Provider2 Wave B disputed/refund seed complete: {Code} → refund {Refund} (advanced {Adv}), balance {Bal} (over-limit {Over}).",
            TxDisputedCode, refund.RefundCode, allocation.PlatformAdvancedRefundAmount, balance.Balance, balance.IsOverLimit());
    }

    /// <summary>
    /// Find-or-create Provider 2's TRY balance with a modest limit (₺5,000) so the seeded ₺10,560 clawback pushes it
    /// OVER the limit (<c>IsOverLimit</c> → the danger negative-balance strip). Uses <see cref="ProviderBalanceEntity"/>
    /// domain methods only. Saved first so its Id exists before the clawback movement is attached.
    /// </summary>
    private async Task<ProviderBalanceEntity> GetOrCreateOverLimitBalanceAsync(CancellationToken ct)
    {
        var balance = await _db.ProviderBalances
            .FirstOrDefaultAsync(b => b.ProviderProfileId == Provider2 && b.CurrencyCode == CurrencyTry, ct);
        if (balance is not null)
        {
            if (balance.NegativeBalanceLimit is 0m or > 5_000m) balance.SetNegativeBalanceLimit(5_000m);
            return balance;
        }

        balance = ProviderBalanceEntity.Create(Provider2, CurrencyTry, negativeBalanceLimit: 5_000m);
        await _db.ProviderBalances.AddAsync(balance, ct);
        await _db.SaveChangesAsync(ct);   // materialise balance.Id for the movement FK
        return balance;
    }

    // ── Helper — mirrors PaymentTransactionMockSeed's escrow builder (tx-level KDV-on-commission model) ──────
    private static PaymentTransactionEntity MakeSrEscrow(
        string code, long srId, long payerId, long providerId,
        decimal gross, decimal commRate, string idempotencyKey)
    {
        var commission = Math.Round(gross * commRate, 2);
        var vat        = Math.Round(commission * 0.20m, 2);
        var net        = gross - commission - vat;

        return PaymentTransactionEntity.Create(
            transactionCode:        code,
            transactionType:        TransactionType.ServiceRequestEscrow,
            contextType:            TransactionContextType.ServiceRequest,
            contextId:              srId,
            contextSubId:           null,
            payerProfileId:         payerId,
            recipientProfileId:     providerId,
            grossAmount:            gross,
            commissionAmount:       commission,
            commissionRateSnapshot: commRate,
            vatOnCommission:        vat,
            netPayoutAmount:        net,
            discountAmount:         0m,
            currencyCode:           CurrencyTry,
            gatewayProvider:        "IYZICO",
            idempotencyKey:         idempotencyKey,
            escrowRequired:         true);
    }
}
