using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.Interface;
using Aizen.Modules.Payment.Abstraction.Model.Result;
using Aizen.Modules.Payment.Application.Commands.SubscribeParticipantForOwner;
using Aizen.Modules.Payment.Application.Queries.GetParticipantSubscriptionPaymentStatusForOwner;
using Aizen.Modules.Payment.Application.Services;
using Aizen.Modules.Payment.Domain.Entities.Plan;
using Aizen.Modules.Payment.Domain.Entities.Subscription;
using Aizen.Modules.Payment.Domain.Entities.Transaction;
using Aizen.Modules.Payment.Repository.Persistence;
using Aizen.Modules.Payment.Repository.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aizen.Modules.Payment.Repository.UnitTests;

/// <summary>
/// BE-MO7 — the owner participant-membership subscribe/status surface over an in-memory DbContext with the real
/// handlers + repositories + the manual gateway. Proves: a FREE plan is applied immediately; a PAID plan initiates a
/// PendingIntent Subscription checkout (the subscription is created later by the capture consumer, not here) at the
/// plan's own price (never a client price); double-subscribe is blocked; a re-initiate reuses the pending checkout;
/// the payment status is owner-scoped and maps the MO3 lifecycle; and the owner result DTOs are cost-free.
/// </summary>
public sealed class ParticipantMembershipMo7Tests
{
    private const long ParticipantA = 700011;
    private const long ParticipantB = 700022;

    private static PaymentDbContext NewDb()
        => new(new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase($"mo7-{Guid.NewGuid():N}").Options);

    private static PaymentGatewayResolver NewGatewayResolver()
    {
        var services = new ServiceCollection();
        services.AddKeyedSingleton<IPaymentGatewayProvider>("manual",
            (_, _) => new ManualPaymentGatewayProvider(NullLogger<ManualPaymentGatewayProvider>.Instance));
        var sp = services.BuildServiceProvider();
        return new PaymentGatewayResolver(sp, NullLogger<PaymentGatewayResolver>.Instance);
    }

    private static SubscribeParticipantForOwnerCommandHandler NewSubscribeHandler(PaymentDbContext db)
        => new(new ParticipantPlanRepository(db), new PaymentTransactionRepository(db), NewGatewayResolver(),
               NullLogger<SubscribeParticipantForOwnerCommandHandler>.Instance);

    private static async Task<ParticipantPlanEntity> SeedPlanAsync(PaymentDbContext db, decimal monthlyPrice, string code)
    {
        var plan = ParticipantPlanEntity.Create(
            planCode: code, name: $"{code} Plan", description: null,
            monthlyPriceTRY: monthlyPrice, annualPriceTRY: null, trialDays: null, badgeLabel: null,
            serviceDiscountRate: 0.10m, cargoDryDiscountRate: 0.05m, inkCoinEarnMultiplier: 1.5m, sortOrder: 1);
        db.ParticipantPlans.Add(plan);
        await db.SaveChangesAsync();
        return plan;
    }

    // (2a) Free/launch plan → applied immediately (no payment).
    [Fact]
    public async Task Subscribe_FreePlan_IsAppliedImmediately_NoTransaction()
    {
        await using var db = NewDb();
        var plan = await SeedPlanAsync(db, monthlyPrice: 0m, code: "LAUNCH");

        var result = await NewSubscribeHandler(db).Handle(
            new SubscribeParticipantForOwnerCommand { ParticipantProfileId = ParticipantA, ParticipantPlanId = plan.Id },
            CancellationToken.None);
        await db.SaveChangesAsync();

        result!.Mode.Should().Be("Immediate");
        result.SubscriptionId.Should().NotBeNull();
        result.TransactionId.Should().BeNull();
        result.Amount.Should().Be(0m);

        var sub = await db.ParticipantSubscriptions.AsNoTracking().SingleAsync();
        sub.ParticipantProfileId.Should().Be(ParticipantA);
        sub.Status.Should().Be(SubscriptionStatus.Active);
        (await db.Transactions.CountAsync()).Should().Be(0, "a free plan moves no money");
    }

    // (2b) Paid plan → a PendingIntent Subscription checkout at the PLAN's price; the subscription is NOT created yet
    // (the capture consumer creates it). Proves the price is server-resolved (never client-supplied).
    [Fact]
    public async Task Subscribe_PaidPlan_InitiatesPendingCheckout_AtPlanPrice_NoSubscriptionYet()
    {
        await using var db = NewDb();
        var plan = await SeedPlanAsync(db, monthlyPrice: 199m, code: "GOLD");

        var result = await NewSubscribeHandler(db).Handle(
            new SubscribeParticipantForOwnerCommand { ParticipantProfileId = ParticipantA, ParticipantPlanId = plan.Id },
            CancellationToken.None);
        await db.SaveChangesAsync();

        result!.Mode.Should().Be("PaymentPending");
        result.TransactionId.Should().NotBeNull();
        result.SubscriptionId.Should().BeNull();
        result.Amount.Should().Be(199m, "the price comes from the plan, never the client");

        var tx = await db.Transactions.AsNoTracking().SingleAsync();
        tx.Status.Should().Be(PaymentTransactionStatus.PendingIntent);
        tx.ContextType.Should().Be(TransactionContextType.Subscription);
        tx.ContextId.Should().Be(plan.Id, "the capture consumer discriminates by ContextId = plan id");
        tx.PayerProfileId.Should().Be(ParticipantA);
        tx.GrossAmount.Should().Be(199m);

        (await db.ParticipantSubscriptions.CountAsync()).Should().Be(0, "the subscription is created on capture, not now");
    }

    // (3) Double-subscribe is blocked by the existing guard.
    [Fact]
    public async Task Subscribe_WhenAlreadyActive_IsBlocked()
    {
        await using var db = NewDb();
        var plan = await SeedPlanAsync(db, monthlyPrice: 0m, code: "LAUNCH");
        var now = DateTime.UtcNow;
        db.ParticipantSubscriptions.Add(ParticipantPlanSubscriptionEntity.Create(
            ParticipantA, plan.Id, 0m, "TRY",
            DateTime.SpecifyKind(now, DateTimeKind.Utc), DateTime.SpecifyKind(now.AddMonths(1), DateTimeKind.Utc),
            autoRenew: true, paymentTransactionId: null, serviceDiscountAtSubscription: 0.10m, earnMultiplierAtSubscription: 1.5m));
        await db.SaveChangesAsync();

        var act = async () => await NewSubscribeHandler(db).Handle(
            new SubscribeParticipantForOwnerCommand { ParticipantProfileId = ParticipantA, ParticipantPlanId = plan.Id },
            CancellationToken.None);

        (await act.Should().ThrowAsync<AizenBusinessException>()).And.ErrorCode
            .Should().Be((int)PaymentErrorCode.SubscriptionAlreadyActive);
    }

    // (2c) Re-initiating a paid checkout for the same (participant, plan) reuses the pending transaction — no double charge.
    [Fact]
    public async Task Subscribe_PaidPlan_ReInitiate_ReusesPendingCheckout()
    {
        await using var db = NewDb();
        var plan = await SeedPlanAsync(db, monthlyPrice: 199m, code: "GOLD");

        var first = await NewSubscribeHandler(db).Handle(
            new SubscribeParticipantForOwnerCommand { ParticipantProfileId = ParticipantA, ParticipantPlanId = plan.Id }, CancellationToken.None);
        await db.SaveChangesAsync();

        var second = await NewSubscribeHandler(db).Handle(
            new SubscribeParticipantForOwnerCommand { ParticipantProfileId = ParticipantA, ParticipantPlanId = plan.Id }, CancellationToken.None);
        await db.SaveChangesAsync();

        second!.TransactionId.Should().Be(first!.TransactionId, "the same (participant, plan) reuses its pending checkout");
        (await db.Transactions.CountAsync()).Should().Be(1, "no second charge is created");
    }

    // (payment status) owner-scoped + the MO3 lifecycle map (Pending → Paid on capture); another participant is rejected.
    [Fact]
    public async Task PaymentStatus_IsOwnerScoped_AndMapsLifecycle()
    {
        await using var db = NewDb();
        var plan = await SeedPlanAsync(db, monthlyPrice: 199m, code: "GOLD");
        await NewSubscribeHandler(db).Handle(
            new SubscribeParticipantForOwnerCommand { ParticipantProfileId = ParticipantA, ParticipantPlanId = plan.Id }, CancellationToken.None);
        await db.SaveChangesAsync();
        var tx = await db.Transactions.AsNoTracking().SingleAsync();

        var handler = new GetParticipantSubscriptionPaymentStatusForOwnerQueryHandler(new PaymentTransactionRepository(db));

        // Pending before capture.
        var pending = await handler.Handle(new GetParticipantSubscriptionPaymentStatusForOwnerQuery
        { ParticipantProfileId = ParticipantA, TransactionId = tx.Id }, CancellationToken.None);
        pending!.Status.Should().Be("Pending");

        // Another participant cannot read it.
        var foreign = async () => await handler.Handle(new GetParticipantSubscriptionPaymentStatusForOwnerQuery
        { ParticipantProfileId = ParticipantB, TransactionId = tx.Id }, CancellationToken.None);
        await foreign.Should().ThrowAsync<AizenBusinessException>();

        // Capture → Paid.
        var live = await db.Transactions.FirstAsync(x => x.Id == tx.Id);
        live.Capture(live.GatewayReference ?? "MANUAL-CAP");
        db.Transactions.Update(live);
        await db.SaveChangesAsync();

        var paid = await handler.Handle(new GetParticipantSubscriptionPaymentStatusForOwnerQuery
        { ParticipantProfileId = ParticipantA, TransactionId = tx.Id }, CancellationToken.None);
        paid!.Status.Should().Be("Paid");
    }

    // (5/6) The owner result DTOs are cost-free — no platform/provider funding split, revenue allocation, commission,
    // net-payout, or margin.
    [Fact]
    public void OwnerResultDtos_AreCostFree()
    {
        var names = typeof(SubscribeParticipantForOwnerResult).GetProperties().Select(p => p.Name)
            .Concat(typeof(ParticipantSubscriptionPaymentStatusResult).GetProperties().Select(p => p.Name))
            .Concat(typeof(ActiveParticipantSubscriptionResult).GetProperties().Select(p => p.Name));

        names.Should().NotContain(n =>
            n.Contains("Funded", StringComparison.OrdinalIgnoreCase) ||
            n.Contains("Funding", StringComparison.OrdinalIgnoreCase) ||
            n.Contains("RevenueAllocation", StringComparison.OrdinalIgnoreCase) ||
            n.Contains("Commission", StringComparison.OrdinalIgnoreCase) ||
            n.Contains("NetPayout", StringComparison.OrdinalIgnoreCase) ||
            n.Contains("Margin", StringComparison.OrdinalIgnoreCase) ||
            n.Contains("PlatformFee", StringComparison.OrdinalIgnoreCase));
    }
}
