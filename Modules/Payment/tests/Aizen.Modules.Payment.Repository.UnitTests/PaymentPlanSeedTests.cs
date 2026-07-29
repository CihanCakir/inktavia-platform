using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.Commission;
using Aizen.Modules.Payment.Repository.Persistence;
using Aizen.Modules.Payment.Repository.Repositories;
using Aizen.Modules.Payment.Repository.Seed;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aizen.Modules.Payment.Repository.UnitTests;

public sealed class PaymentPlanSeedTests
{
    private static PaymentDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase($"payment-seed-{Guid.NewGuid():N}")
            .Options;
        return new PaymentDbContext(options);
    }

    private static async Task<decimal?> ResolveForPlanAsync(PaymentDbContext db, string planCode)
    {
        var plan = await db.ProviderPlans.AsNoTracking().FirstAsync(p => p.PlanCode == planCode);
        var repo = new CommissionRuleRepository(db);
        var res  = await repo.ResolveAsync(new CommissionResolveContext(ProviderPlanId: plan.Id), DateTime.UtcNow);
        return res?.Rate;
    }

    [Fact]
    public async Task Seed_Produces_Plan_Rates_15_12_9_And_A_Global()
    {
        await using var db = NewDb();
        var seed = new PaymentPlanSeed(db, NullLogger<PaymentPlanSeed>.Instance);

        await seed.SeedAsync();

        (await ResolveForPlanAsync(db, "FREE")).Should().Be(0.15m);
        (await ResolveForPlanAsync(db, "STANDARD")).Should().Be(0.12m);
        (await ResolveForPlanAsync(db, "PREMIUM_PARTNER")).Should().Be(0.09m);

        var globals = await db.CommissionRules
            .Where(r => r.RuleType == CommissionRuleType.Global && r.RuleCode == null)
            .ToListAsync();
        globals.Should().HaveCount(1);
        globals[0].CommissionRate.Should().Be(0.15m);
    }

    [Fact]
    public async Task Seed_Is_Idempotent_No_Duplicates_On_Rerun()
    {
        await using var db = NewDb();
        var seed = new PaymentPlanSeed(db, NullLogger<PaymentPlanSeed>.Instance);

        await seed.SeedAsync();
        var countAfterFirst = await db.CommissionRules.CountAsync();

        await seed.SeedAsync();   // re-run
        var countAfterSecond = await db.CommissionRules.CountAsync();

        countAfterSecond.Should().Be(countAfterFirst, "re-seeding must not create duplicate rules");

        // Rates still correct and unique per plan after the second run.
        (await ResolveForPlanAsync(db, "FREE")).Should().Be(0.15m);
        (await ResolveForPlanAsync(db, "PREMIUM_PARTNER")).Should().Be(0.09m);
    }
}
