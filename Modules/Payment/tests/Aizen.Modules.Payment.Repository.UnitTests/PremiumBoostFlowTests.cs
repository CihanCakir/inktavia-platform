using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Application.Commands.CapturePayment;
using Aizen.Modules.Payment.Application.Gateway.Iyzico;
using Aizen.Modules.Payment.Application.Gateway.Iyzico.Models;
using Aizen.Modules.Payment.Application.Services;
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
/// BE-P11 §9.2/§13.9 — the premium boost lifecycle over an in-memory DbContext with the real repositories +
/// PremiumBoostService: purchase (Pending, no entitlement) → webhook (Active, idempotent) → refund (Revoked, no negative
/// balance) → expiration. Plus the non-marketplace split-guard skip and the commission-decoupling guarantee (§19.4).
/// </summary>
public sealed class PremiumBoostFlowTests
{
    private const long ProviderId = 501;
    private const long OfferId    = 9001;
    private static readonly DateTime T0 = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

    private static PaymentDbContext NewDb()
        => new(new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase($"pbf-{Guid.NewGuid():N}").Options);

    private static PremiumBoostService NewService(PaymentDbContext db, Aizen.Core.Messagebus.Abstraction.Senders.IAizenMessagePublisher? publisher = null)
        => new(new PremiumPurchaseRepository(db), new PremiumEntitlementRepository(db),
               new FinancialLedgerPostingService(new FinancialLedgerRepository(db), NullLogger<FinancialLedgerPostingService>.Instance),
               publisher ?? new RecordingPublisher(),
               NullLogger<PremiumBoostService>.Instance);

    private static async Task SeedAsync(PaymentDbContext db)
        => await new PremiumProductSeed(db, NullLogger<PremiumProductSeed>.Instance).SeedAsync();

    /// <summary>Builds a Pending premium purchase + its PendingIntent boost transaction (as PurchaseOfferBoost would).</summary>
    private static async Task<(PremiumPurchaseEntity purchase, PaymentTransactionEntity tx)> InitiatePurchaseAsync(
        PaymentDbContext db, decimal price = 149.90m)
    {
        var product = await db.PremiumProducts.FirstAsync(x => x.Code == PremiumProductSeed.OfferBoostCode);
        var resolvedPrice = await db.PremiumProductPrices.FirstAsync(x => x.PremiumProductId == product.Id);

        var purchase = PremiumPurchaseEntity.Create(
            ProviderId, product.Id, product.Code, resolvedPrice.Id, price, "TRY", product.DurationDays, OfferId, "BST-1");
        db.PremiumPurchases.Add(purchase);
        await db.SaveChangesAsync();

        var tx = PaymentTransactionEntity.Create(
            transactionCode: $"TXN-{Guid.NewGuid():N}"[..20], transactionType: TransactionType.PremiumBoostPurchase,
            contextType: TransactionContextType.Premium, contextId: OfferId, contextSubId: null,
            payerProfileId: ProviderId, recipientProfileId: null, grossAmount: price,
            commissionAmount: 0m, commissionRateSnapshot: 0m, vatOnCommission: 0m, netPayoutAmount: 0m,
            discountAmount: 0m, currencyCode: "TRY", gatewayProvider: "manual",
            idempotencyKey: $"BOOST-{ProviderId}-OFFER-{OfferId}", escrowRequired: false);
        tx.AttachGatewayReference($"MANUAL-BOOST-{OfferId}");
        db.Transactions.Add(tx);
        await db.SaveChangesAsync();

        purchase.LinkTransaction(tx.Id);
        db.PremiumPurchases.Update(purchase);
        await db.SaveChangesAsync();
        return (purchase, tx);
    }

    // ── Not active before webhook ───────────────────────────────────────────────

    [Fact]
    public async Task Purchase_IsPending_WithNoEntitlement_UntilWebhook()
    {
        await using var db = NewDb();
        await SeedAsync(db);
        var (purchase, _) = await InitiatePurchaseAsync(db);

        purchase.Status.Should().Be(PremiumPurchaseStatus.Pending);
        (await db.PremiumEntitlements.CountAsync()).Should().Be(0, "§9.2 — no entitlement before the success webhook");
    }

    // ── Webhook → Active (idempotent, one entitlement) ──────────────────────────

    [Fact]
    public async Task Webhook_ActivatesOneEntitlement_AndIsIdempotent()
    {
        await using var db = NewDb();
        await SeedAsync(db);
        var (purchase, tx) = await InitiatePurchaseAsync(db);
        tx.Capture(tx.GatewayReference);   // the webhook captures first
        db.Transactions.Update(tx);

        var svc = NewService(db);
        await svc.OnBoostPaidAsync(tx, CancellationToken.None);
        await db.SaveChangesAsync();

        var e = await db.PremiumEntitlements.AsNoTracking().SingleAsync();
        e.Status.Should().Be(PremiumEntitlementStatus.Active);
        e.ContextRef.Should().Be(OfferId);
        e.ExpiresAt!.Value.Should().BeCloseTo(e.StartsAt!.Value.AddDays(7), TimeSpan.FromMinutes(1));   // now → now+7d
        (await db.PremiumPurchases.AsNoTracking().FirstAsync(x => x.Id == purchase.Id)).Status.Should().Be(PremiumPurchaseStatus.Paid);

        // Duplicate webhook → still exactly one entitlement.
        await svc.OnBoostPaidAsync(tx, CancellationToken.None);
        await db.SaveChangesAsync();
        (await db.PremiumEntitlements.CountAsync()).Should().Be(1, "duplicate webhook must be idempotent (unique PremiumPurchaseId)");
    }

    // N4 (§9): boost paid emits PremiumBoostActivatedMessage to the provider; the subsequent refund
    // of an Active entitlement emits PremiumBoostRevokedMessage. Fire-and-forget, additive to the core op.
    [Fact]
    public async Task BoostPaid_EmitsActivated_AndRefund_EmitsRevoked()
    {
        await using var db = NewDb();
        await SeedAsync(db);
        var (purchase, tx) = await InitiatePurchaseAsync(db);
        tx.Capture(tx.GatewayReference);
        db.Transactions.Update(tx);

        var pub = new RecordingPublisher();
        var svc = NewService(db, pub);

        await svc.OnBoostPaidAsync(tx, CancellationToken.None);
        await db.SaveChangesAsync();

        var activated = pub.Published.OfType<Aizen.Modules.Payment.Abstraction.Message.PremiumBoostActivatedMessage>().ToList();
        activated.Should().ContainSingle("boost paid must emit exactly one PremiumBoostActivatedMessage");
        activated[0].OfferId.Should().Be(OfferId);
        activated[0].ProviderProfileId.Should().NotBe(0);

        await svc.OnBoostRefundedAsync(tx, "customer refund", CancellationToken.None);
        await db.SaveChangesAsync();

        var revoked = pub.Published.OfType<Aizen.Modules.Payment.Abstraction.Message.PremiumBoostRevokedMessage>().ToList();
        revoked.Should().ContainSingle("refunding an active boost must emit exactly one PremiumBoostRevokedMessage");
        revoked[0].OfferId.Should().Be(OfferId);
    }

    // ── Manual capture → Active (gateway-agnostic activation) ───────────────────
    // FIX_BOOST_ACTIVATE_MANUAL_GATEWAY: the manual admin capture path must fire OnBoostPaidAsync just like the iyzico
    // webhook, so a boost activates under PAYMENT_GATEWAY_ACTIVE=manual (no iyzico). A second capture is a no-op.

    [Fact]
    public async Task ManualCapture_OfBoostTx_ActivatesOneEntitlement_AndSecondCaptureIsNoOp()
    {
        await using var db = NewDb();
        await SeedAsync(db);
        var (purchase, tx) = await InitiatePurchaseAsync(db);

        var handler = new CapturePaymentCommandHandler(
            unitOfWork: null!,   // unused by the handler body (SaveChanges is the decorator's job)
            new PaymentTransactionRepository(db),
            NewService(db),
            NullLogger<CapturePaymentCommandHandler>.Instance);

        var cmd = new CapturePaymentCommand
        {
            TransactionId    = tx.Id,
            GatewayReference = $"MANUAL-CAP-{tx.Id}",
            PaidAmount       = 149.90m,
            CurrencyCode     = "TRY",
        };

        // First capture → captures the tx AND activates exactly one entitlement; purchase → Paid.
        var first = await handler.Handle(cmd, CancellationToken.None);
        await db.SaveChangesAsync();   // the AizenCommandHandlerDecorator saves in production; the test does it explicitly

        first!.WasAlreadyCaptured.Should().BeFalse();
        var e = await db.PremiumEntitlements.AsNoTracking().SingleAsync();
        e.Status.Should().Be(PremiumEntitlementStatus.Active);
        e.ContextRef.Should().Be(OfferId);
        e.ExpiresAt!.Value.Should().BeCloseTo(e.StartsAt!.Value.AddDays(7), TimeSpan.FromMinutes(1));   // now → now+7d
        (await db.PremiumPurchases.AsNoTracking().FirstAsync(x => x.Id == purchase.Id)).Status
            .Should().Be(PremiumPurchaseStatus.Paid);

        // Second capture on the same tx → idempotent (CapturedAt guard); NO duplicate entitlement.
        var second = await handler.Handle(cmd, CancellationToken.None);
        await db.SaveChangesAsync();
        second!.WasAlreadyCaptured.Should().BeTrue();
        (await db.PremiumEntitlements.CountAsync()).Should().Be(1, "a second capture must not create a duplicate entitlement");
    }

    // ── Non-boost capture activates nothing ─────────────────────────────────────

    [Fact]
    public async Task ManualCapture_OfNonBoostTx_ActivatesNoEntitlement()
    {
        await using var db = NewDb();
        await SeedAsync(db);

        var tx = PaymentTransactionEntity.Create(
            transactionCode: $"TXN-{Guid.NewGuid():N}"[..20], transactionType: TransactionType.ServiceRequestEscrow,
            contextType: TransactionContextType.ServiceRequest, contextId: 7001, contextSubId: null,
            payerProfileId: ProviderId, recipientProfileId: 999, grossAmount: 500m,
            commissionAmount: 50m, commissionRateSnapshot: 0.10m, vatOnCommission: 0m, netPayoutAmount: 450m,
            discountAmount: 0m, currencyCode: "TRY", gatewayProvider: "manual",
            idempotencyKey: $"SR-{Guid.NewGuid():N}"[..20], escrowRequired: true);
        db.Transactions.Add(tx);
        await db.SaveChangesAsync();

        var handler = new CapturePaymentCommandHandler(
            unitOfWork: null!, new PaymentTransactionRepository(db), NewService(db),
            NullLogger<CapturePaymentCommandHandler>.Instance);

        var result = await handler.Handle(new CapturePaymentCommand
        {
            TransactionId = tx.Id, GatewayReference = $"MANUAL-CAP-{tx.Id}", PaidAmount = 500m, CurrencyCode = "TRY",
        }, CancellationToken.None);
        await db.SaveChangesAsync();

        result!.WasAlreadyCaptured.Should().BeFalse();
        (await db.Transactions.AsNoTracking().FirstAsync(x => x.Id == tx.Id)).CapturedAt.Should().NotBeNull();
        (await db.PremiumEntitlements.CountAsync()).Should().Be(0, "a non-boost capture must not touch premium boost");
    }

    // ── Refund → Revoked, no ProviderNegativeBalance ────────────────────────────

    [Fact]
    public async Task Refund_RevokesEntitlement_AndCreatesNoProviderNegativeBalance()
    {
        await using var db = NewDb();
        await SeedAsync(db);
        var (_, tx) = await InitiatePurchaseAsync(db);
        tx.Capture(tx.GatewayReference);
        db.Transactions.Update(tx);

        var svc = NewService(db);
        await svc.OnBoostPaidAsync(tx, CancellationToken.None);
        await db.SaveChangesAsync();

        await svc.OnBoostRefundedAsync(tx, "customer_refund", CancellationToken.None);
        await db.SaveChangesAsync();

        var e = await db.PremiumEntitlements.AsNoTracking().SingleAsync();
        e.Status.Should().Be(PremiumEntitlementStatus.Revoked);
        e.RevokedAt.Should().NotBeNull();
        (await db.PremiumPurchases.AsNoTracking().SingleAsync()).Status.Should().Be(PremiumPurchaseStatus.Refunded);
        (await db.ProviderBalances.CountAsync()).Should().Be(0, "§9.2 — a boost refund is non-marketplace: no provider clawback");

        // Idempotent replay.
        await svc.OnBoostRefundedAsync(tx, "customer_refund", CancellationToken.None);
        await db.SaveChangesAsync();
        (await db.PremiumEntitlements.AsNoTracking().SingleAsync()).Status.Should().Be(PremiumEntitlementStatus.Revoked);
    }

    // ── Expiration ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task ExpireDue_TransitionsPastExpiryToExpired()
    {
        await using var db = NewDb();
        await SeedAsync(db);
        var (purchase, tx) = await InitiatePurchaseAsync(db);
        tx.Capture(tx.GatewayReference);
        db.Transactions.Update(tx);
        var svc = NewService(db);
        await svc.OnBoostPaidAsync(tx, CancellationToken.None);
        await db.SaveChangesAsync();

        // Nothing due now; everything due far in the future (past the 7-day window).
        (await svc.ExpireDueAsync(DateTime.UtcNow, 100, CancellationToken.None)).Should().Be(0);
        var expired = await svc.ExpireDueAsync(DateTime.UtcNow.AddDays(30), 100, CancellationToken.None);
        expired.Should().Be(1);
        (await db.PremiumEntitlements.AsNoTracking().SingleAsync()).Status.Should().Be(PremiumEntitlementStatus.Expired);
    }

    // ── Duplicate active boost guard (repository) ───────────────────────────────

    [Fact]
    public async Task DuplicateActiveBoost_IsDetectedByRepository()
    {
        await using var db = NewDb();
        await SeedAsync(db);
        var (_, _) = await InitiatePurchaseAsync(db);

        var product = await db.PremiumProducts.FirstAsync();
        var repo = new PremiumPurchaseRepository(db);
        (await repo.GetActiveOrPendingAsync(ProviderId, OfferId, product.Id, CancellationToken.None))
            .Should().NotBeNull("a Pending boost already exists for (provider, offer)");
    }

    // ── Non-marketplace checkout: split guard skips (no submerchant items) ───────

    [Fact]
    public void NonMarketplaceCheckout_SplitGuard_Skips()
    {
        // A premium basket: whole amount to the main merchant, no SubMerchantKey/SubMerchantPrice.
        var basket = IyzicoBasketBuilder.BuildSingle("TXN-1", "Offer boost", customerTotal: 149.90m, providerNetTotal: 0m, subMerchantKey: null);

        // requireSplit=false → the split-sum assertion is skipped; only Σ basket.Price == customerTotal is checked.
        var act = () => IyzicoSplitMathGuard.Verify(
            customerTotal: 149.90m, providerNetTotal: 0m, basket: basket, requireSplit: false, expectedRetained: null);
        act.Should().NotThrow();

        basket.Should().OnlyContain(i => string.IsNullOrEmpty(i.SubMerchantPrice), "non-marketplace → no split lines");
    }

    // ── Commission decoupling (§19.4, binding) ──────────────────────────────────
    // Premium must not be queryable from the commission/economics path: a boosted offer's commission is identical to a
    // non-boosted one because the commission resolvers never reference any premium type. Enforced structurally — no
    // commission-path service constructor may depend on a premium repository.

    [Fact]
    public void CommissionPathServices_DoNotDependOnAnyPremiumRepository()
    {
        var commissionPathTypes = new[]
        {
            typeof(CommissionCalculationService),
            typeof(ProviderCommissionBenefitService),
            typeof(CommissionBenefitEntitlementService),
            typeof(ServiceRequestPaymentEconomicsCalculationService),
        };

        foreach (var t in commissionPathTypes)
        {
            var ctorParamTypes = t.GetConstructors()
                .SelectMany(c => c.GetParameters())
                .Select(p => p.ParameterType.Name);
            ctorParamTypes.Should().NotContain(n => n.Contains("Premium"),
                $"{t.Name} must never reference premium (§19.4 — boost ≠ commission)");
        }
    }

    // ── Rounding (MoneyMath) applied to the snapshotted unit price ──────────────

    [Fact]
    public void UnitPrice_IsRounded_2dp()
    {
        var p = PremiumPurchaseEntity.Create(ProviderId, 1, "OFFER_BOOST_7D", 5,
            unitPriceSnapshot: Aizen.Modules.Payment.Domain.Money.MoneyMath.Round(149.905m),
            currencyCodeSnapshot: "TRY", durationDaysSnapshot: 7, contextRef: OfferId, purchaseCode: "BST-R");
        p.UnitPriceSnapshot.Should().Be(149.91m);
    }
}
