using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.PlatformFee;
using Aizen.Modules.Payment.Repository.Persistence;
using Aizen.Modules.Payment.Repository.Repositories;
using Aizen.Modules.Payment.Repository.Seed;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aizen.Modules.Payment.Repository.UnitTests;

public sealed class PlatformFeeRuleRepositoryTests
{
    private static PaymentDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase($"pfr-{Guid.NewGuid():N}")
            .Options;
        return new PaymentDbContext(options);
    }

    private static readonly DateTime From = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static PlatformFeeRuleEntity Bounds(string? code = null)
        => PlatformFeeRuleEntity.Create(
            PlatformFeeModel.PercentageWithBounds, 0.025m, null, 99m, 1500m,
            "TRY", null, null, CommissionRulePriority.Standard, From, null, code);

    private static PlatformFeeRuleEntity Percentage(string? code = null, string? customerType = null)
        => PlatformFeeRuleEntity.Create(
            PlatformFeeModel.Percentage, 0.03m, null, null, null,
            "TRY", null, customerType, CommissionRulePriority.Standard, From, null, code);

    private static PlatformFeeRuleEntity Fixed(string? code = null)
        => PlatformFeeRuleEntity.Create(
            PlatformFeeModel.Fixed, null, 99m, null, null,
            "TRY", null, null, CommissionRulePriority.Standard, From, null, code);

    // ── Pure resolve: NO writes ──────────────────────────────────────────────────

    [Fact]
    public async Task ResolveAsync_Performs_No_Writes()
    {
        await using var db = NewDb();
        db.PlatformFeeRules.Add(Bounds("PFR-1"));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var repo = new PlatformFeeRuleRepository(db);
        var res = await repo.ResolveAsync(new PlatformFeeResolveContext("TRY"), DateTime.UtcNow);

        res.Should().NotBeNull();
        res!.Model.Should().Be(PlatformFeeModel.PercentageWithBounds);
        db.ChangeTracker.Entries().Should().BeEmpty("resolve must be side-effect-free (AsNoTracking, no writes)");
    }

    [Fact]
    public async Task FindOverlappingActiveRuleAsync_Detects_Overlap()
    {
        await using var db = NewDb();
        db.PlatformFeeRules.Add(Bounds("PFR-EXIST"));
        await db.SaveChangesAsync();

        var candidate = Bounds(null); // same scope (Global/TRY) + Standard priority + overlapping window
        var repo = new PlatformFeeRuleRepository(db);

        (await repo.FindOverlappingActiveRuleAsync(candidate)).Should().NotBeNull();
    }

    // ── Seed: %2.5 / 99 / 1500 resolves for TRY; idempotent ──────────────────────

    [Fact]
    public async Task Seed_Creates_Bounds_Rule_Resolvable_For_TRY()
    {
        await using var db = NewDb();
        var seed = new PlatformFeeRuleSeed(db, NullLogger<PlatformFeeRuleSeed>.Instance);

        await seed.SeedAsync();

        var repo = new PlatformFeeRuleRepository(db);
        var res = await repo.ResolveAsync(new PlatformFeeResolveContext("TRY"), DateTime.UtcNow);

        res.Should().NotBeNull();
        res!.Model.Should().Be(PlatformFeeModel.PercentageWithBounds);
        res.Rate.Should().Be(0.025m);
        res.MinAmount.Should().Be(99m);
        res.MaxAmount.Should().Be(1500m);
    }

    [Fact]
    public async Task Seed_Is_Idempotent_On_Rerun()
    {
        await using var db = NewDb();
        var seed = new PlatformFeeRuleSeed(db, NullLogger<PlatformFeeRuleSeed>.Instance);

        await seed.SeedAsync();
        var afterFirst = await db.PlatformFeeRules.CountAsync();

        await seed.SeedAsync();
        var afterSecond = await db.PlatformFeeRules.CountAsync();

        afterFirst.Should().Be(1);
        afterSecond.Should().Be(1, "re-seeding must not create a duplicate default rule");
    }

    // ── List (paging + filter) / detail (BE-P3 admin surface) ────────────────────

    [Fact]
    public async Task GetByIdAsync_Returns_Rule_And_Null_When_Missing()
    {
        await using var db = NewDb();
        var rule = Bounds("PFR-DETAIL");
        db.PlatformFeeRules.Add(rule);
        await db.SaveChangesAsync();

        var repo = new PlatformFeeRuleRepository(db);

        (await repo.GetByIdAsync(rule.Id))!.RuleCode.Should().Be("PFR-DETAIL");
        (await repo.GetByIdAsync(999_999)).Should().BeNull();
    }

    [Fact]
    public async Task GetPagedAsync_Filters_By_Model()
    {
        await using var db = NewDb();
        db.PlatformFeeRules.Add(Bounds("PFR-B"));
        db.PlatformFeeRules.Add(Percentage("PFR-P"));
        db.PlatformFeeRules.Add(Fixed("PFR-F"));
        await db.SaveChangesAsync();

        var repo = new PlatformFeeRuleRepository(db);
        var (items, total) = await repo.GetPagedAsync(
            PlatformFeeModel.Percentage, null, null, null, null, 0, 20);

        total.Should().Be(1);
        items.Should().ContainSingle().Which.Model.Should().Be(PlatformFeeModel.Percentage);
    }

    [Fact]
    public async Task GetPagedAsync_Pages_And_Reports_Total()
    {
        await using var db = NewDb();
        db.PlatformFeeRules.Add(Bounds("PFR-1"));
        db.PlatformFeeRules.Add(Percentage("PFR-2", customerType: "GOLD"));
        db.PlatformFeeRules.Add(Fixed("PFR-3"));
        await db.SaveChangesAsync();

        var repo = new PlatformFeeRuleRepository(db);
        var (items, total) = await repo.GetPagedAsync(null, null, null, null, null, skip: 1, take: 1);

        total.Should().Be(3, "Total reflects the full filtered set, not the page");
        items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetPagedAsync_Filters_By_CustomerType()
    {
        await using var db = NewDb();
        db.PlatformFeeRules.Add(Percentage("PFR-GLOBAL"));
        db.PlatformFeeRules.Add(Percentage("PFR-GOLD", customerType: "GOLD"));
        await db.SaveChangesAsync();

        var repo = new PlatformFeeRuleRepository(db);
        var (items, total) = await repo.GetPagedAsync(null, null, null, null, "gold", 0, 20);

        total.Should().Be(1);
        items.Should().ContainSingle().Which.CustomerType.Should().Be("GOLD");
    }

    // ── Reactivate conflict guard: an inactive rule that would collide when re-activated ──

    [Fact]
    public async Task FindOverlappingActiveRuleAsync_Detects_Reactivation_Conflict()
    {
        await using var db = NewDb();
        var active = Bounds("PFR-ACTIVE");                 // Global / TRY / Standard / open window
        var toReactivate = Bounds("PFR-INACTIVE");         // same scope-key + priority + overlapping window
        toReactivate.Deactivate();
        db.PlatformFeeRules.Add(active);
        db.PlatformFeeRules.Add(toReactivate);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var repo = new PlatformFeeRuleRepository(db);

        // The reactivate handler runs exactly this guard before flipping IsActive back on.
        var conflict = await repo.FindOverlappingActiveRuleAsync(toReactivate);

        conflict.Should().NotBeNull();
        conflict!.RuleCode.Should().Be("PFR-ACTIVE");
    }
}
