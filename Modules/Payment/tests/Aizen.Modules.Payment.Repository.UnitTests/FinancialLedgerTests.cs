using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Application.Queries.GetFinancialSummaryReport;
using Aizen.Modules.Payment.Application.Services;
using Aizen.Modules.Payment.Domain.Entities.Economics;
using Aizen.Modules.Payment.Domain.Entities.Premium;
using Aizen.Modules.Payment.Domain.Entities.Transaction;
using Aizen.Modules.Payment.Repository.Persistence;
using Aizen.Modules.Payment.Repository.Repositories;
using Aizen.Modules.Payment.Repository.Seed;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aizen.Modules.Payment.Repository.UnitTests;

/// <summary>
/// BE-P12 §15/§19.17 — the append-only financial ledger, posted from the immutable sources over an in-memory DbContext:
/// posting amounts == source, §19.17 rules (no merge, provider-funded=memo, platform-funded=expense, VAT≠revenue),
/// idempotency + backfill re-run no-dup, NetMarketplaceContribution formula, reconciliation, rounding.
/// Snapshot (no discount): Service 5000 @0.12 → commission 600, feeNet 145 / feeVat 29 / feeGross 174; PlatformGrossShare 774.
/// </summary>
public sealed class FinancialLedgerTests
{
    private static readonly DateTime T0 = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

    private static PaymentDbContext NewDb()
        => new(new DbContextOptionsBuilder<PaymentDbContext>().UseInMemoryDatabase($"led-{Guid.NewGuid():N}").Options);

    private static FinancialLedgerPostingService NewPosting(PaymentDbContext db)
        => new(new FinancialLedgerRepository(db), NullLogger<FinancialLedgerPostingService>.Instance);

    private static PaymentEconomicsSnapshotEntity Snapshot(long ctx = 42)
    {
        var line = new LineEconomicsInput("L1", 1, 1, GrossBeforeDiscount: 5000m, CustomerDiscount: 0m,
            ProviderFundedDiscount: 0m, PlatformFundedDiscount: 0m, LineCommissionEligibility.Eligible,
            CommissionBase: 5000m, CommissionRate: 0.12m, CommissionAmount: 600m, ProviderNet: 4400m, LineVat: 0m,
            LineTotal: 5000m, RuleId: null, RuleCode: "STD", Commissionable: true);
        var fee = new PlatformFeeInput(3, 0.025m, 99m, 1500m, 5000m, 145m, 29m, 174m);
        return PaymentEconomicsSnapshotEntity.CreateFromLines(ctx, "TRY", new[] { line }, fee, 5000m);
    }

    /// <summary>Discounted snapshot: CustomerDiscount 250 = providerFunded 100 + platformFunded 150 (§19.17 split).</summary>
    private static PaymentEconomicsSnapshotEntity DiscountedSnapshot()
    {
        var line = new LineEconomicsInput("L1", 1, 1, GrossBeforeDiscount: 5000m, CustomerDiscount: 250m,
            ProviderFundedDiscount: 100m, PlatformFundedDiscount: 150m, LineCommissionEligibility.Eligible,
            CommissionBase: 4750m, CommissionRate: 0.12m, CommissionAmount: 570m, ProviderNet: 4180m, LineVat: 0m,
            LineTotal: 4750m, RuleId: null, RuleCode: "X", Commissionable: true);
        var fee = new PlatformFeeInput(null, 0m, 0m, 0m, 4750m, 0m, 0m, 0m);
        return PaymentEconomicsSnapshotEntity.CreateFromLines(43, "TRY", new[] { line }, fee, 4750m);
    }

    private static PaymentTransactionEntity Tx(long snapshotId, decimal gross, long provider = 77, long customer = 9)
    {
        var tx = PaymentTransactionEntity.Create(
            transactionCode: $"TXN-{Guid.NewGuid():N}"[..20], transactionType: TransactionType.ServiceRequestEscrow,
            contextType: TransactionContextType.ServiceRequest, contextId: 1, contextSubId: 2,
            payerProfileId: customer, recipientProfileId: provider, grossAmount: gross,
            commissionAmount: 600m, commissionRateSnapshot: 0.12m, vatOnCommission: 0m, netPayoutAmount: 4400m,
            discountAmount: 0m, currencyCode: "TRY", gatewayProvider: "manual", idempotencyKey: Guid.NewGuid().ToString("N"),
            escrowRequired: true);
        tx.Capture("GW");
        tx.LinkEconomicsSnapshot(snapshotId);
        return tx;
    }

    private static decimal Line(PaymentDbContext db, LedgerAccountLine line)
        => db.FinancialLedgerEntries.Where(x => x.AccountLine == line).Sum(x => x.IsReversal ? -x.Amount : x.Amount);

    // ── Acceptance posting: amounts == snapshot ─────────────────────────────────

    [Fact]
    public async Task PostAcceptance_PostsRevenueLiabilityContribution_MatchingSnapshot()
    {
        await using var db = NewDb();
        var s = Snapshot(); db.Add(s); await db.SaveChangesAsync();
        var tx = Tx(s.Id, 5174m); db.Add(tx); await db.SaveChangesAsync();

        await NewPosting(db).PostAcceptanceAsync(tx, s, CancellationToken.None);
        await db.SaveChangesAsync();

        Line(db, LedgerAccountLine.ProviderCommissionRevenue).Should().Be(600m);
        Line(db, LedgerAccountLine.CustomerPlatformFeeNetRevenue).Should().Be(145m);
        Line(db, LedgerAccountLine.CustomerPlatformFeeVatLiability).Should().Be(29m);   // §19.17 liability, not revenue
        Line(db, LedgerAccountLine.PlatformGrossShare).Should().Be(774m);              // §13.10 = commission + feeGross
        Line(db, LedgerAccountLine.ProviderSideContribution).Should().Be(600m);
        Line(db, LedgerAccountLine.CustomerSideContribution).Should().Be(145m);
        Line(db, LedgerAccountLine.NetMarketplaceContribution).Should().Be(745m);      // 600 + 145 − 0

        // Provider/customer ids are carried from the transaction (the snapshot is offer-scoped).
        var rev = await db.FinancialLedgerEntries.FirstAsync(x => x.AccountLine == LedgerAccountLine.ProviderCommissionRevenue);
        rev.ProviderProfileId.Should().Be(77);
        rev.CustomerProfileId.Should().Be(9);
        rev.SourceType.Should().Be(LedgerSourceType.AcceptanceSnapshot);
    }

    // ── §19.17 discount rules: platform-funded = expense; provider-funded = memo (NOT expense) ──

    [Fact]
    public async Task DiscountRules_PlatformFundedIsExpense_ProviderFundedIsMemo()
    {
        await using var db = NewDb();
        var s = DiscountedSnapshot(); db.Add(s); await db.SaveChangesAsync();
        var tx = Tx(s.Id, 4750m); db.Add(tx); await db.SaveChangesAsync();

        await NewPosting(db).PostAcceptanceAsync(tx, s, CancellationToken.None);
        await db.SaveChangesAsync();

        var platformFunded = await db.FinancialLedgerEntries.FirstAsync(x => x.AccountLine == LedgerAccountLine.PlatformFundedCustomerDiscountExpense);
        platformFunded.Amount.Should().Be(150m);
        platformFunded.Nature.Should().Be(LedgerEntryNature.Expense);              // IS an Inktavia campaign cost

        var providerFunded = await db.FinancialLedgerEntries.FirstAsync(x => x.AccountLine == LedgerAccountLine.ProviderFundedCustomerDiscount);
        providerFunded.Amount.Should().Be(100m);
        providerFunded.Nature.Should().Be(LedgerEntryNature.Memo);                 // §19.17 — NOT an Inktavia expense
        providerFunded.ContributionSigned().Should().Be(0m);

        // The two are never merged into one DiscountAmount.
        (await db.FinancialLedgerEntries.CountAsync(x =>
            x.AccountLine == LedgerAccountLine.PlatformFundedCustomerDiscountExpense
         || x.AccountLine == LedgerAccountLine.ProviderFundedCustomerDiscount)).Should().Be(2);
    }

    // ── Idempotency: re-posting the same source is a no-op ──────────────────────

    [Fact]
    public async Task PostAcceptance_IsIdempotent()
    {
        await using var db = NewDb();
        var s = Snapshot(); db.Add(s); await db.SaveChangesAsync();
        var tx = Tx(s.Id, 5174m); db.Add(tx); await db.SaveChangesAsync();
        var posting = NewPosting(db);

        await posting.PostAcceptanceAsync(tx, s, CancellationToken.None);
        await db.SaveChangesAsync();
        var count1 = await db.FinancialLedgerEntries.CountAsync();

        await posting.PostAcceptanceAsync(tx, s, CancellationToken.None);   // re-post (duplicate webhook / backfill)
        await db.SaveChangesAsync();
        (await db.FinancialLedgerEntries.CountAsync()).Should().Be(count1, "unique (SourceType,SourceRef,AccountLine,IsReversal) → no dup");
    }

    // ── NetMarketplaceContribution formula + VAT/provider-funded shown separately ──

    [Fact]
    public async Task Summary_NetMarketplaceContribution_ExcludesVatAndProviderFundedDiscount()
    {
        await using var db = NewDb();
        var s = DiscountedSnapshot(); db.Add(s); await db.SaveChangesAsync();
        var tx = Tx(s.Id, 4750m); db.Add(tx); await db.SaveChangesAsync();
        await NewPosting(db).PostAcceptanceAsync(tx, s, CancellationToken.None);
        await db.SaveChangesAsync();

        var handler = new GetFinancialSummaryReportQueryHandler(new FinancialLedgerRepository(db));
        var now = DateTime.UtcNow;
        var r = await handler.Handle(new GetFinancialSummaryReportQuery { From = now.AddDays(-1), To = now.AddDays(1), Currency = "TRY" }, CancellationToken.None);

        // Revenue = commission 570 (+ feeNet 0 here); Expense = platform-funded discount 150. VAT = 0, providerFunded 100.
        r!.RevenueTotal.Should().Be(570m);
        r.ExpenseTotal.Should().Be(150m);
        r.NetMarketplaceContribution.Should().Be(420m);                    // 570 − 150 (VAT + provider-funded excluded)
        r.ProviderFundedDiscountTotal.Should().Be(100m);                   // surfaced separately (not in the P&L)
        r.Breakdown.Should().Contain(b => b.AccountLine == LedgerAccountLine.ProviderFundedCustomerDiscount && b.Total == 100m);
    }

    // ── Reconciliation: Σ ledger per source == the source's own amounts ─────────

    [Fact]
    public async Task Reconciliation_LedgerMatchesSnapshotToTheCent()
    {
        await using var db = NewDb();
        var s = Snapshot(); db.Add(s); await db.SaveChangesAsync();
        var tx = Tx(s.Id, 5174m); db.Add(tx); await db.SaveChangesAsync();
        await NewPosting(db).PostAcceptanceAsync(tx, s, CancellationToken.None);
        await db.SaveChangesAsync();

        var bySource = await new FinancialLedgerRepository(db).GetBySourceAsync(LedgerSourceType.AcceptanceSnapshot, s.Id, CancellationToken.None);
        bySource.First(e => e.AccountLine == LedgerAccountLine.ProviderCommissionRevenue).Amount.Should().Be(s.CommissionAmountSnapshot);
        bySource.First(e => e.AccountLine == LedgerAccountLine.CustomerPlatformFeeNetRevenue).Amount.Should().Be(s.PlatformFeeNetAmountSnapshot);
        bySource.First(e => e.AccountLine == LedgerAccountLine.CustomerPlatformFeeVatLiability).Amount.Should().Be(s.PlatformFeeVatAmountSnapshot);
        bySource.First(e => e.AccountLine == LedgerAccountLine.PlatformGrossShare).Amount.Should().Be(s.PlatformGrossShareSnapshot);
    }

    // ── Premium revenue posting ─────────────────────────────────────────────────

    [Fact]
    public async Task PostPremiumPaid_PostsPremiumProductRevenue()
    {
        await using var db = NewDb();
        var purchase = PremiumPurchaseEntity.Create(77, 1, "OFFER_BOOST_7D", 5, 149.90m, "TRY", 7, 9001, "BST-1");
        db.PremiumPurchases.Add(purchase); await db.SaveChangesAsync();

        await NewPosting(db).PostPremiumPaidAsync(purchase, 555, CancellationToken.None);
        await db.SaveChangesAsync();

        var e = await db.FinancialLedgerEntries.SingleAsync(x => x.AccountLine == LedgerAccountLine.PremiumProductRevenue);
        e.Amount.Should().Be(149.90m);
        e.Nature.Should().Be(LedgerEntryNature.Revenue);
        e.SourceType.Should().Be(LedgerSourceType.PremiumPurchase);
    }

    // ── Backfill: populates once; re-run no duplicates ──────────────────────────

    [Fact]
    public async Task Backfill_PopulatesOnce_AndReRunIsIdempotent()
    {
        await using var db = NewDb();
        var s = Snapshot(); db.Add(s); await db.SaveChangesAsync();
        var tx = Tx(s.Id, 5174m); db.Add(tx); await db.SaveChangesAsync();
        var purchase = PremiumPurchaseEntity.Create(77, 1, "OFFER_BOOST_7D", 5, 149.90m, "TRY", 7, 9001, "BST-1");
        purchase.LinkTransaction(999); purchase.MarkPaid();
        db.PremiumPurchases.Add(purchase); await db.SaveChangesAsync();

        var backfill = new FinancialLedgerBackfillService(db, NewPosting(db), NullLogger<FinancialLedgerBackfillService>.Instance);
        var added1 = await backfill.BackfillAsync(CancellationToken.None);
        added1.Should().BeGreaterThan(0);
        var total = await db.FinancialLedgerEntries.CountAsync();

        var added2 = await backfill.BackfillAsync(CancellationToken.None);
        added2.Should().Be(0, "a backfill re-run must not duplicate");
        (await db.FinancialLedgerEntries.CountAsync()).Should().Be(total);
        Line(db, LedgerAccountLine.PremiumProductRevenue).Should().Be(149.90m);
    }

    // ── Provider + participant subscriptions with OVERLAPPING ids must not collide ──
    // (independent id sequences share SourceType.Subscription → the distinct plan account line keeps their keys unique).

    [Fact]
    public async Task Backfill_ProviderAndParticipantSubs_WithSameId_DoNotCollide()
    {
        await using var db = NewDb();
        var provSub = Domain.Entities.Subscription.ProviderPlanSubscriptionEntity.Create(
            providerProfileId: 7, providerPlanId: 1, paidAmount: 299m, currencyCode: "TRY",
            periodStart: T0, periodEnd: T0.AddMonths(1), autoRenew: true, paymentTransactionId: null,
            commissionRateAtSubscription: 0m);
        var partSub = Domain.Entities.Subscription.ParticipantPlanSubscriptionEntity.Create(
            participantProfileId: 9, participantPlanId: 1, paidAmount: 99m, currencyCode: "TRY",
            periodStart: T0, periodEnd: T0.AddMonths(1), autoRenew: true, paymentTransactionId: null,
            serviceDiscountAtSubscription: 0m, earnMultiplierAtSubscription: 1m);
        db.ProviderSubscriptions.Add(provSub);
        db.ParticipantSubscriptions.Add(partSub);
        await db.SaveChangesAsync();
        provSub.Id.Should().Be(partSub.Id, "in-memory sequences overlap — the collision case we must survive");

        var backfill = new FinancialLedgerBackfillService(db, NewPosting(db), NullLogger<FinancialLedgerBackfillService>.Instance);
        var act = async () => await backfill.BackfillAsync(CancellationToken.None);
        await act.Should().NotThrowAsync();

        Line(db, LedgerAccountLine.ProviderPlanRevenue).Should().Be(299m);
        Line(db, LedgerAccountLine.CustomerPlanRevenue).Should().Be(99m);
    }
}
