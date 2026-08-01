using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Application.Services;
using Aizen.Modules.Payment.Domain.Entities.ProfitProtection;
using Aizen.Modules.Payment.Repository.Persistence;
using Aizen.Modules.Payment.Repository.Repositories;
using Aizen.Modules.Payment.Repository.Seed;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aizen.Modules.Payment.Repository.UnitTests;

public sealed class ProfitProtectionTests
{
    private static PaymentDbContext NewDb()
        => new(new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase($"pp-{Guid.NewGuid():N}").Options);

    private static ProfitProtectionCalculationService Service(PaymentDbContext db)
        => new(new ProfitProtectionPolicyRepository(db),
               new ProfitProtectionEvaluationLogRepository(db),
               NullLogger<ProfitProtectionCalculationService>.Instance);

    private static ProfitProtectionContext Ctx(decimal requestedDiscount, decimal budget)
        => new("TRY",
            ServiceAmount: 10000m, CustomerPayableServiceAmount: 10000m, CustomerTotalAmount: 9250m, ProviderNetAmount: 9100m,
            ProviderCommissionNetRevenue: 900m, CustomerPlatformFeeNetRevenue: 250m,
            RequestedPlatformFundedCustomerDiscount: requestedDiscount,
            CustomerBenefitBudgetRemaining: budget);

    // ── Approved → no eval log (no snapshot on success either; snapshot = P8) ────

    [Fact]
    public async Task Profitable_Context_Is_Approved_And_Not_Logged()
    {
        await using var db = NewDb();
        await new ProfitProtectionPolicySeed(db, NullLogger<ProfitProtectionPolicySeed>.Instance).SeedAsync();

        var ev = await Service(db).EvaluateAsync(Ctx(requestedDiscount: 0m, budget: 0m));

        ev.State.Should().Be(ProfitProtectionDecisionState.Approved);
        (await db.ProfitProtectionEvaluationLogs.CountAsync()).Should().Be(0, "Approved outcomes are not logged");
    }

    // ── Non-Approved → eval log written; NO snapshot on failure (§7) ─────────────

    [Fact]
    public async Task Loss_Context_Is_Adjusted_And_Logged()
    {
        await using var db = NewDb();
        await new ProfitProtectionPolicySeed(db, NullLogger<ProfitProtectionPolicySeed>.Instance).SeedAsync();

        var ev = await Service(db).EvaluateAsync(Ctx(requestedDiscount: 1000m, budget: 2000m));

        ev.State.Should().Be(ProfitProtectionDecisionState.ApprovedWithAdjustment);
        ev.AppliedPlatformFundedDiscount.Should().BeLessThan(1000m);

        var logs = await db.ProfitProtectionEvaluationLogs.ToListAsync();
        logs.Should().HaveCount(1);
        logs[0].DecisionState.Should().Be(ProfitProtectionDecisionState.ApprovedWithAdjustment);
    }

    [Fact]
    public async Task No_Policy_Yields_ConfigurationError_And_Logged()
    {
        await using var db = NewDb();   // no policy seeded

        var ev = await Service(db).EvaluateAsync(Ctx(requestedDiscount: 0m, budget: 0m));

        ev.State.Should().Be(ProfitProtectionDecisionState.ConfigurationError);
        (await db.ProfitProtectionEvaluationLogs.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Conflicting_Policies_Yield_ConfigurationError()
    {
        await using var db = NewDb();
        var from = DateTime.UtcNow.AddDays(-1);
        db.ProfitProtectionPolicies.Add(MakePolicy(from, "PPOL-A"));
        db.ProfitProtectionPolicies.Add(MakePolicy(from, "PPOL-B")); // overlapping active → conflict
        await db.SaveChangesAsync();

        var ev = await Service(db).EvaluateAsync(Ctx(requestedDiscount: 0m, budget: 0m));

        ev.State.Should().Be(ProfitProtectionDecisionState.ConfigurationError);
        (await db.ProfitProtectionEvaluationLogs.CountAsync()).Should().Be(1);
    }

    // ── Seed idempotency ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Seed_Is_Idempotent()
    {
        await using var db = NewDb();
        var seed = new ProfitProtectionPolicySeed(db, NullLogger<ProfitProtectionPolicySeed>.Instance);

        await seed.SeedAsync();
        await seed.SeedAsync();

        (await db.ProfitProtectionPolicies.CountAsync()).Should().Be(1);
    }

    // ── Admin list / detail / reactivate surface (BE-P5 P5 additions) ────────────

    [Fact]
    public async Task GetByIdAsync_Returns_Policy_And_Null_When_Missing()
    {
        await using var db = NewDb();
        var p = MakePolicy(DateTime.UtcNow.AddDays(-1), "PPOL-DETAIL", "TRY");
        db.ProfitProtectionPolicies.Add(p);
        await db.SaveChangesAsync();

        var repo = new ProfitProtectionPolicyRepository(db);

        (await repo.GetByIdAsync(p.Id))!.PolicyCode.Should().Be("PPOL-DETAIL");
        (await repo.GetByIdAsync(999_999)).Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_Returns_Every_Version_For_The_History()
    {
        await using var db = NewDb();
        db.ProfitProtectionPolicies.Add(MakePolicy(DateTime.UtcNow.AddDays(-10), "PPOL-USD", "USD"));
        db.ProfitProtectionPolicies.Add(MakePolicy(DateTime.UtcNow.AddDays(-5),  "PPOL-TRY1", "TRY"));
        var inactive = MakePolicy(DateTime.UtcNow.AddDays(-20), "PPOL-TRY0", "TRY");
        inactive.Deactivate();
        db.ProfitProtectionPolicies.Add(inactive);
        await db.SaveChangesAsync();

        var repo = new ProfitProtectionPolicyRepository(db);
        var all = await repo.GetAllAsync();

        all.Should().HaveCount(3, "the version history includes inactive/historical versions, not just the active one");
    }

    // Reactivate re-runs the single-active guard: an inactive policy that would collide when reactivated.
    [Fact]
    public async Task FindOverlappingActivePolicyAsync_Detects_Reactivation_Conflict()
    {
        await using var db = NewDb();
        var from = DateTime.UtcNow.AddDays(-1);
        var active = MakePolicy(from, "PPOL-ACTIVE", "TRY");             // active TRY policy, open window
        var toReactivate = MakePolicy(from, "PPOL-INACTIVE", "TRY");     // same currency + overlapping window
        toReactivate.Deactivate();
        db.ProfitProtectionPolicies.Add(active);
        db.ProfitProtectionPolicies.Add(toReactivate);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var repo = new ProfitProtectionPolicyRepository(db);

        // The reactivate handler runs exactly this guard before flipping IsActive back on.
        var conflict = await repo.FindOverlappingActivePolicyAsync(toReactivate);

        conflict.Should().NotBeNull();
        conflict!.PolicyCode.Should().Be("PPOL-ACTIVE");
    }

    [Fact]
    public void Reactivate_Sets_Active_And_Rederives_Status()
    {
        var p = MakePolicy(DateTime.UtcNow.AddDays(-1), "PPOL-RE", "TRY");
        p.Deactivate();
        p.IsActive.Should().BeFalse();

        p.Reactivate();

        p.IsActive.Should().BeTrue();
        p.Status.Should().Be(CommissionRuleStatus.Active);
    }

    private static ProfitProtectionPolicyEntity MakePolicy(DateTime from, string code, string currency = "TRY")
        => ProfitProtectionPolicyEntity.Create(
            currency, 0m, 0m, 0m, 0m, 10m, 0.01m, 0.029m, 0.25m, 0.005m, 0m, 0m, 0.5m,
            ProfitProtectionAdjustmentOrder.PlatformDiscountThenCommissionBenefit, from, null, code);
}
