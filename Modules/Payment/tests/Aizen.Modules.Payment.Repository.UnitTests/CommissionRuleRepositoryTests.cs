using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.Commission;
using Aizen.Modules.Payment.Repository.Persistence;
using Aizen.Modules.Payment.Repository.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Payment.Repository.UnitTests;

public sealed class CommissionRuleRepositoryTests
{
    private static PaymentDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase($"payment-{Guid.NewGuid():N}")
            .Options;
        return new PaymentDbContext(options);
    }

    private static readonly DateTime From = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    // ── Pure resolve: NO writes; applied-count untouched ─────────────────────────

    [Fact]
    public async Task ResolveAsync_Performs_No_Writes_AppliedCount_Unchanged()
    {
        await using var db = NewDb();
        var global = CommissionRuleEntity.CreateGlobal(0.15m, From, null, CommissionRulePriority.Standard, null, null);
        db.CommissionRules.Add(global);
        await db.SaveChangesAsync();

        var repo = new CommissionRuleRepository(db);
        var res = await repo.ResolveAsync(new CommissionResolveContext(), DateTime.UtcNow);

        res.Should().NotBeNull();
        res!.Rate.Should().Be(0.15m);

        var reloaded = await db.CommissionRules.AsNoTracking().FirstAsync(x => x.Id == global.Id);
        reloaded.ResolvedAppliedCount.Should().Be(0, "resolve must be side-effect-free");
    }

    [Fact]
    public async Task MarkAppliedAsync_Increments_Exactly_Once()
    {
        await using var db = NewDb();
        var rule = CommissionRuleEntity.CreateGlobal(0.15m, From, null, CommissionRulePriority.Standard, null, null);
        db.CommissionRules.Add(rule);
        await db.SaveChangesAsync();

        var repo = new CommissionRuleRepository(db);
        await repo.MarkAppliedAsync(rule.Id);

        var reloaded = await db.CommissionRules.AsNoTracking().FirstAsync(x => x.Id == rule.Id);
        reloaded.ResolvedAppliedCount.Should().Be(1);
    }

    [Fact]
    public async Task MarkAppliedAsync_Throws_When_Rule_Missing()
    {
        await using var db = NewDb();
        var repo = new CommissionRuleRepository(db);

        Func<Task> act = () => repo.MarkAppliedAsync(999);

        await act.Should().ThrowAsync<AizenBusinessException>()
           .Where(e => e.ErrorCode == (int)PaymentErrorCode.CommissionRuleNotFound);
    }

    // ── Create-time conflict guard via the repository ────────────────────────────

    [Fact]
    public async Task FindOverlappingActiveRuleAsync_Detects_Overlap()
    {
        await using var db = NewDb();
        var existing = CommissionRuleEntity.CreateForPlan(3, 0.12m, From, null, CommissionRulePriority.Standard, null, "EXIST");
        db.CommissionRules.Add(existing);
        await db.SaveChangesAsync();

        var candidate = CommissionRuleEntity.CreateForPlan(3, 0.10m, From, null, CommissionRulePriority.Standard, null, null);

        var repo = new CommissionRuleRepository(db);
        var conflict = await repo.FindOverlappingActiveRuleAsync(candidate);

        conflict.Should().NotBeNull();
        conflict!.Id.Should().Be(existing.Id);
    }

    [Fact]
    public async Task FindOverlappingActiveRuleAsync_Null_When_No_Conflict()
    {
        await using var db = NewDb();
        var existing = CommissionRuleEntity.CreateForPlan(3, 0.12m, From, null, CommissionRulePriority.Standard, null, "EXIST");
        db.CommissionRules.Add(existing);
        await db.SaveChangesAsync();

        // Different plan → different scope-key → no conflict.
        var candidate = CommissionRuleEntity.CreateForPlan(4, 0.10m, From, null, CommissionRulePriority.Standard, null, null);

        var repo = new CommissionRuleRepository(db);
        (await repo.FindOverlappingActiveRuleAsync(candidate)).Should().BeNull();
    }

    // ── Backward-compat wrapper resolves the right plan rate ─────────────────────

    [Fact]
    public async Task ResolveRateAsync_Wrapper_Returns_Matched_Plan_Rate()
    {
        await using var db = NewDb();
        db.CommissionRules.Add(CommissionRuleEntity.CreateGlobal(0.15m, From, null, CommissionRulePriority.Standard, null, null));
        db.CommissionRules.Add(CommissionRuleEntity.CreateForPlan(2, 0.12m, From, null, CommissionRulePriority.Standard, null, "STD"));
        await db.SaveChangesAsync();

        var repo = new CommissionRuleRepository(db);
        var rate = await repo.ResolveRateAsync(null, 2, null, DateTime.UtcNow, default);

        rate.Should().Be(0.12m);
    }
}
