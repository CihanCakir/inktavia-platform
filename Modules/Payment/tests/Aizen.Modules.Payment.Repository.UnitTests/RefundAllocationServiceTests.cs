using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Application.Commands.RecordChargeback;
using Aizen.Modules.Payment.Application.Services;
using Aizen.Modules.Payment.Domain.Entities.Economics;
using Aizen.Modules.Payment.Domain.Entities.Transaction;
using Aizen.Modules.Payment.Repository.Persistence;
using Aizen.Modules.Payment.Repository.Repositories;
using Aizen.Modules.Payment.Repository.Seed;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aizen.Modules.Payment.Repository.UnitTests;

/// <summary>
/// BE-P10 — end-to-end orchestration over an in-memory DbContext with the real repositories: snapshot-driven allocation,
/// §7.3 release-after clawback into the provider negative-balance ledger, benefit-restore-once, chargeback + idempotency.
/// Snapshot: Service 5000 @0.12 → commission 600, providerNet 4400; fee net 145 / vat 29 / gross 174; CustomerTotal 5174.
/// </summary>
public sealed class RefundAllocationServiceTests
{
    private const long ProviderId = 77;

    private static PaymentDbContext NewDb()
        => new(new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase($"ras-{Guid.NewGuid():N}").Options);

    private static PaymentEconomicsSnapshotEntity Snapshot(decimal serviceGross = 5000m)
    {
        var line = new LineEconomicsInput(
            LineRef: "L1", ItemType: 1, PricingMethod: 1,
            GrossBeforeDiscount: serviceGross, CustomerDiscount: 0m, ProviderFundedDiscount: 0m, PlatformFundedDiscount: 0m,
            CommissionEligibility: LineCommissionEligibility.Eligible,
            CommissionBase: serviceGross, CommissionRate: 0.12m, CommissionAmount: serviceGross * 0.12m,
            ProviderNet: serviceGross - serviceGross * 0.12m, LineVat: 0m, LineTotal: serviceGross,
            RuleId: null, RuleCode: "STD", Commissionable: true);
        var fee = new PlatformFeeInput(RuleId: 3, Rate: 0.025m, Minimum: 99m, Maximum: 1500m, Base: serviceGross, Net: 145m, Vat: 29m, Gross: 174m);
        return PaymentEconomicsSnapshotEntity.CreateFromLines(42, "TRY", new[] { line }, fee, serviceGross);
    }

    private static PaymentTransactionEntity ReleasedTx(long snapshotId, decimal gross = 5174m)
    {
        var tx = PaymentTransactionEntity.Create(
            transactionCode: $"TXN-{Guid.NewGuid():N}"[..20], transactionType: TransactionType.ServiceRequestEscrow,
            contextType: TransactionContextType.ServiceRequest, contextId: 1, contextSubId: 2,
            payerProfileId: 9, recipientProfileId: ProviderId, grossAmount: gross,
            commissionAmount: 600m, commissionRateSnapshot: 0.12m, vatOnCommission: 0m, netPayoutAmount: 4400m,
            discountAmount: 0m, currencyCode: "TRY", gatewayProvider: "manual", idempotencyKey: Guid.NewGuid().ToString("N"),
            escrowRequired: true);
        tx.Capture("GW-REF");
        tx.Release();                        // ReleasedAt set → release-after path
        tx.LinkEconomicsSnapshot(snapshotId);
        return tx;
    }

    private static FinancialLedgerPostingService NewPosting(PaymentDbContext db)
        => new(new FinancialLedgerRepository(db), NullLogger<FinancialLedgerPostingService>.Instance);

    private static RefundAllocationService NewService(PaymentDbContext db)
        => new(new PaymentEconomicsSnapshotRepository(db), new RefundAllocationPolicyRepository(db),
               new RefundAllocationRepository(db), new ProviderBalanceRepository(db),
               NewPosting(db), NullLogger<RefundAllocationService>.Instance);

    private static async Task<long> SeedPolicyAsync(PaymentDbContext db)
    {
        await new RefundAllocationPolicySeed(db, NullLogger<RefundAllocationPolicySeed>.Instance).SeedAsync();
        return 0;
    }

    // ── Release-after full refund → clawback provider net into the negative-balance ledger + restore once ──

    [Fact]
    public async Task ReleaseAfter_FullRefund_ClawsBackProviderNet_AndRestoresOnce()
    {
        await using var db = NewDb();
        await SeedPolicyAsync(db);
        var snap = Snapshot();
        db.Add(snap); await db.SaveChangesAsync();
        var tx = ReleasedTx(snap.Id);
        db.Add(tx); await db.SaveChangesAsync();

        var record = TransactionRefundRecord.Create(tx.Id, "REF-1", 5174m, "TRY", RefundType.Full, RefundReason.OrganizerCancel, null);
        record.MarkProcessed("GW-RF-1", null);
        db.Add(record);

        var alloc = await NewService(db).ApplyAsync(
            tx, record, requestedRefundAmount: 5174m, RefundCause.ProviderCancelled, restoreBenefit: true, CancellationToken.None);
        await db.SaveChangesAsync();

        alloc.Should().NotBeNull();
        alloc!.ProviderNetReversalAmount.Should().Be(4400m);
        alloc.CommissionRevenueReversalAmount.Should().Be(600m);
        alloc.PlatformAdvancedRefundAmount.Should().Be(4400m);          // platform fronts the provider net
        alloc.RemainingProviderNegativeBalance.Should().Be(4400m);

        var balance = await db.ProviderBalances.AsNoTracking().FirstAsync(x => x.ProviderProfileId == ProviderId);
        balance.Balance.Should().Be(-4400m);
        balance.NegativeAmount.Should().Be(4400m);

        var persisted = await db.RefundAllocations.AsNoTracking().FirstAsync(x => x.RefundRecordId == record.Id);
        persisted.ServiceRefundAmount.Should().Be(5000m);
        persisted.PlatformFeeGrossRefundAmount.Should().Be(174m);

        var savedRecord = await db.Set<TransactionRefundRecord>().AsNoTracking().FirstAsync(x => x.Id == record.Id);
        savedRecord.BenefitRestoreApplied.Should().BeTrue();
        savedRecord.RefundAllocationId.Should().Be(persisted.Id);
        savedRecord.Cause.Should().Be(RefundCause.ProviderCancelled);
        savedRecord.ReleaseState.Should().Be(ReleaseState.AfterProviderRelease);
    }

    // ── Release-before refund → no clawback, no negative balance ─────────────────

    [Fact]
    public async Task ReleaseBefore_FullRefund_NoClawback()
    {
        await using var db = NewDb();
        await SeedPolicyAsync(db);
        var snap = Snapshot();
        db.Add(snap); await db.SaveChangesAsync();

        // Captured (NOT released) → release-before
        var tx = PaymentTransactionEntity.Create(
            transactionCode: "TXN-CB", transactionType: TransactionType.ServiceRequestEscrow,
            contextType: TransactionContextType.ServiceRequest, contextId: 1, contextSubId: 2,
            payerProfileId: 9, recipientProfileId: ProviderId, grossAmount: 5174m,
            commissionAmount: 600m, commissionRateSnapshot: 0.12m, vatOnCommission: 0m, netPayoutAmount: 4400m,
            discountAmount: 0m, currencyCode: "TRY", gatewayProvider: "manual", idempotencyKey: Guid.NewGuid().ToString("N"),
            escrowRequired: true);
        tx.Capture("GW-REF");
        tx.LinkEconomicsSnapshot(snap.Id);
        db.Add(tx); await db.SaveChangesAsync();

        var record = TransactionRefundRecord.Create(tx.Id, "REF-2", 5174m, "TRY", RefundType.Full, RefundReason.ServiceRequestCancelled, null);
        record.MarkProcessed("GW-RF-2", null);
        db.Add(record);

        var alloc = await NewService(db).ApplyAsync(
            tx, record, requestedRefundAmount: 5174m, RefundCause.CustomerCancelledBeforeWork, restoreBenefit: true, CancellationToken.None);
        await db.SaveChangesAsync();

        alloc!.ProviderRecoveryAmount.Should().Be(0m);
        alloc.PlatformAdvancedRefundAmount.Should().Be(0m);
        (await db.ProviderBalances.CountAsync()).Should().Be(0, "release-before cancels escrow — nothing to claw back");
    }

    // ── Chargeback → release-after recovery + expense, idempotent on the gateway reference ──

    [Fact]
    public async Task Chargeback_RecoversProviderNet_AndIsIdempotent()
    {
        await using var db = NewDb();
        await SeedPolicyAsync(db);
        var snap = Snapshot();
        db.Add(snap); await db.SaveChangesAsync();
        var tx = ReleasedTx(snap.Id);
        db.Add(tx); await db.SaveChangesAsync();

        var handler = new RecordChargebackCommandHandler(
            new PaymentTransactionRepository(db), new ChargebackRecordRepository(db),
            new PaymentEconomicsSnapshotRepository(db), new RefundAllocationPolicyRepository(db),
            new ProviderBalanceRepository(db), NewPosting(db), NullLogger<RecordChargebackCommandHandler>.Instance);

        var first = await handler.Handle(new RecordChargebackCommand
        {
            TransactionId = tx.Id, GatewayChargebackReference = "CB-1", ChargebackExpenseAmount = 10m,
        }, CancellationToken.None);
        await db.SaveChangesAsync();

        first!.AlreadyProcessed.Should().BeFalse();
        first.ProviderRecoveredAmount.Should().Be(4400m);
        first.ChargebackExpenseAmount.Should().Be(10m);
        first.ChargebackAmount.Should().Be(5174m);                      // defaulted to the full gross

        var txAfter = await db.Transactions.AsNoTracking().FirstAsync(x => x.Id == tx.Id);
        txAfter.DisputedAt.Should().NotBeNull();
        txAfter.Status.Should().Be(PaymentTransactionStatus.Disputed);
        (await db.ProviderBalances.AsNoTracking().FirstAsync(x => x.ProviderProfileId == ProviderId)).Balance.Should().Be(-4400m);

        // Idempotent replay — same gateway reference, no second clawback.
        var second = await handler.Handle(new RecordChargebackCommand
        {
            TransactionId = tx.Id, GatewayChargebackReference = "CB-1", ChargebackExpenseAmount = 10m,
        }, CancellationToken.None);
        await db.SaveChangesAsync();

        second!.AlreadyProcessed.Should().BeTrue();
        (await db.ChargebackRecords.CountAsync()).Should().Be(1);
        (await db.ProviderBalances.AsNoTracking().FirstAsync(x => x.ProviderProfileId == ProviderId)).Balance
            .Should().Be(-4400m, "a duplicate chargeback must not claw back a second time");
    }
}
