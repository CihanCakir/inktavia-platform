using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.Plan;
using Aizen.Modules.Payment.Domain.Entities.Subscription;
using Aizen.Modules.Payment.Repository.Persistence;
using Aizen.Modules.Payment.Repository.Repositories;
using Aizen.Modules.Payment.Repository.Seed;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aizen.Modules.Payment.Repository.UnitTests;

public sealed class ProviderPlanPriceTests
{
    private static readonly DateTime GoLive    = new(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime LaunchEnd = new(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static PaymentDbContext NewDb()
        => new(new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase($"ppp-{Guid.NewGuid():N}").Options);

    private static IConfiguration Config() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Payment:GoLiveDateUtc"] = "2026-07-01T00:00:00Z" })
            .Build();

    private static async Task<(long freeId, long stdId, long premId)> SeedPlansAsync(PaymentDbContext db)
    {
        var free = ProviderPlanEntity.Create("FREE", "Free", null, 0m, null, null, null, 5, false, false, 1);
        var std  = ProviderPlanEntity.Create("STANDARD", "Standard", null, 499m, null, null, null, null, false, true, 2);
        var prem = ProviderPlanEntity.Create("PREMIUM_PARTNER", "Premium", null, 999m, null, null, null, null, true, true, 3);
        db.ProviderPlans.AddRange(free, std, prem);
        await db.SaveChangesAsync();
        return (free.Id, std.Id, prem.Id);
    }

    // ── Seed: contiguous Launch/List, resolution + boundary ──────────────────────

    [Fact]
    public async Task Seed_Creates_Contiguous_Rows_And_Resolves_By_Instant()
    {
        await using var db = NewDb();
        var (freeId, stdId, _) = await SeedPlansAsync(db);
        await new ProviderPlanPriceSeed(db, Config(), NullLogger<ProviderPlanPriceSeed>.Instance).SeedAsync();

        var repo = new ProviderPlanPriceRepository(db);

        // STANDARD in launch window → 499; at/after +6mo → 1490 (upper-exclusive boundary).
        (await repo.ResolveAsync(stdId, "TRY", BillingPeriod.Monthly, new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc)))!
            .PriceAmount.Should().Be(499m);
        (await repo.ResolveAsync(stdId, "TRY", BillingPeriod.Monthly, LaunchEnd))!
            .PriceAmount.Should().Be(1490m);

        // Contiguity: launch.EffectiveTo == list.EffectiveFrom.
        var rows = await repo.GetByPlanAsync(stdId);
        var launch = rows.Single(r => r.PriceType == ProviderPlanPriceType.Launch);
        var list   = rows.Single(r => r.PriceType == ProviderPlanPriceType.List);
        launch.EffectiveTo.Should().Be(list.EffectiveFrom);

        // FREE resolves 0 any time.
        (await repo.ResolveAsync(freeId, "TRY", BillingPeriod.Monthly, new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc)))!
            .PriceAmount.Should().Be(0m);
    }

    [Fact]
    public async Task Seed_Is_Idempotent()
    {
        await using var db = NewDb();
        await SeedPlansAsync(db);
        var seed = new ProviderPlanPriceSeed(db, Config(), NullLogger<ProviderPlanPriceSeed>.Instance);

        await seed.SeedAsync();
        var afterFirst = await db.ProviderPlanPrices.CountAsync();
        await seed.SeedAsync();
        var afterSecond = await db.ProviderPlanPrices.CountAsync();

        afterFirst.Should().Be(5);  // STANDARD(2) + PREMIUM(2) + FREE(1)
        afterSecond.Should().Be(5, "re-seeding must not duplicate rows");
    }

    // ── Global launch: same price for two different plans at the same instant ────

    [Fact]
    public async Task Launch_Window_Is_Global_Calendar_Not_Per_Provider()
    {
        await using var db = NewDb();
        var (_, stdId, premId) = await SeedPlansAsync(db);
        await new ProviderPlanPriceSeed(db, Config(), NullLogger<ProviderPlanPriceSeed>.Instance).SeedAsync();
        var repo = new ProviderPlanPriceRepository(db);

        var instant = new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc); // same calendar instant for both
        (await repo.ResolveAsync(stdId,  "TRY", BillingPeriod.Monthly, instant))!.PriceAmount.Should().Be(499m);
        (await repo.ResolveAsync(premId, "TRY", BillingPeriod.Monthly, instant))!.PriceAmount.Should().Be(999m);
    }

    // ── Renewal price resolution ─────────────────────────────────────────────────

    [Fact]
    public async Task ResolveRenewalPrice_Uses_Price_Active_At_Renewal()
    {
        await using var db = NewDb();
        var (_, stdId, _) = await SeedPlansAsync(db);
        await new ProviderPlanPriceSeed(db, Config(), NullLogger<ProviderPlanPriceSeed>.Instance).SeedAsync();
        var repo = new ProviderPlanPriceRepository(db);

        // Sub priced at launch (499), renewing after launch → renewal resolves the List price (1490).
        var sub = ProviderPlanSubscriptionEntity.Create(
            providerProfileId: 1, providerPlanId: stdId, paidAmount: 499m, currencyCode: "TRY",
            periodStart: GoLive, periodEnd: LaunchEnd.AddMonths(1),
            autoRenew: true, paymentTransactionId: null, commissionRateAtSubscription: 0m);

        var renewal = await repo.ResolveRenewalPriceAsync(sub, sub.SubscriptionPeriodEnd);
        renewal!.PriceAmount.Should().Be(1490m);
    }

    // ── Create-time overlap/gap guard via repository ─────────────────────────────

    [Fact]
    public async Task ValidateInsertable_Detects_Overlap_And_Gap()
    {
        await using var db = NewDb();
        var (_, stdId, _) = await SeedPlansAsync(db);
        db.ProviderPlanPrices.Add(ProviderPlanPriceEntity.Create(
            stdId, ProviderPlanPriceType.Launch, BillingPeriod.Monthly, 499m, "TRY", GoLive, LaunchEnd, "P1"));
        await db.SaveChangesAsync();
        var repo = new ProviderPlanPriceRepository(db);

        var overlapping = ProviderPlanPriceEntity.Create(
            stdId, ProviderPlanPriceType.List, BillingPeriod.Monthly, 1490m, "TRY",
            GoLive.AddMonths(3), LaunchEnd.AddMonths(3), null);
        (await repo.ValidateInsertableAsync(overlapping)).Outcome.Should().Be(PlanPriceGuardOutcome.Overlap);

        var gapped = ProviderPlanPriceEntity.Create(
            stdId, ProviderPlanPriceType.List, BillingPeriod.Monthly, 1490m, "TRY",
            LaunchEnd.AddMonths(1), null, null);
        (await repo.ValidateInsertableAsync(gapped)).Outcome.Should().Be(PlanPriceGuardOutcome.Gap);

        var contiguous = ProviderPlanPriceEntity.Create(
            stdId, ProviderPlanPriceType.List, BillingPeriod.Monthly, 1490m, "TRY", LaunchEnd, null, null);
        (await repo.ValidateInsertableAsync(contiguous)).Outcome.Should().Be(PlanPriceGuardOutcome.Ok);
    }
}
