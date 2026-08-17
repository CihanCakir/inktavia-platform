using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.CustomerDiscount;
using Aizen.Modules.Payment.Repository.Persistence;
using Aizen.Modules.Payment.Repository.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Payment.Repository.UnitTests;

/// <summary>
/// Repository-level coverage for the BE-P6 admin list / detail / reactivate surface of the customer-discount rule
/// (mirrors <see cref="ProfitProtectionTests"/>' P5 admin-surface tests).
/// </summary>
public sealed class CustomerDiscountRuleRepositoryTests
{
    private static PaymentDbContext NewDb()
        => new(new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase($"cdr-{Guid.NewGuid():N}").Options);

    [Fact]
    public async Task GetByIdAsync_Returns_Rule_And_Null_When_Missing()
    {
        await using var db = NewDb();
        var r = MakeRule(DateTime.UtcNow.AddDays(-1), "CDR-DETAIL");
        db.CustomerDiscountRules.Add(r);
        await db.SaveChangesAsync();

        var repo = new CustomerDiscountRuleRepository(db);

        (await repo.GetByIdAsync(r.Id))!.RuleCode.Should().Be("CDR-DETAIL");
        (await repo.GetByIdAsync(999_999)).Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_Returns_Every_Version_Including_Inactive()
    {
        await using var db = NewDb();
        db.CustomerDiscountRules.Add(MakeRule(DateTime.UtcNow.AddDays(-10), "CDR-USD", "USD"));
        db.CustomerDiscountRules.Add(MakeRule(DateTime.UtcNow.AddDays(-5),  "CDR-TRY1"));
        var inactive = MakeRule(DateTime.UtcNow.AddDays(-20), "CDR-TRY0");
        inactive.Deactivate();
        db.CustomerDiscountRules.Add(inactive);
        await db.SaveChangesAsync();

        var repo = new CustomerDiscountRuleRepository(db);
        var all = await repo.GetAllAsync();

        all.Should().HaveCount(3, "the list includes inactive/historical rules, not just the active ones");
    }

    // Reactivate re-runs the overlap guard: an inactive rule that would collide when reactivated.
    [Fact]
    public async Task FindOverlappingActiveRuleAsync_Detects_Reactivation_Conflict()
    {
        await using var db = NewDb();
        var from = DateTime.UtcNow.AddDays(-1);
        var active       = MakeRule(from, "CDR-ACTIVE");           // active global TRY rule, open window
        var toReactivate = MakeRule(from, "CDR-INACTIVE");         // same scope-key + priority + overlapping window
        toReactivate.Deactivate();
        db.CustomerDiscountRules.Add(active);
        db.CustomerDiscountRules.Add(toReactivate);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var repo = new CustomerDiscountRuleRepository(db);

        // The reactivate handler runs exactly this guard before flipping IsActive back on.
        var conflict = await repo.FindOverlappingActiveRuleAsync(toReactivate);

        conflict.Should().NotBeNull();
        conflict!.RuleCode.Should().Be("CDR-ACTIVE");
    }

    [Fact]
    public void Reactivate_Sets_Active_And_Rederives_Status()
    {
        var r = MakeRule(DateTime.UtcNow.AddDays(-1), "CDR-RE");
        r.Deactivate();
        r.IsActive.Should().BeFalse();
        r.Status.Should().Be(CommissionRuleStatus.Inactive);

        r.Reactivate();

        r.IsActive.Should().BeTrue();
        r.Status.Should().Be(CommissionRuleStatus.Active);
    }

    private static CustomerDiscountRuleEntity MakeRule(DateTime from, string code, string currency = "TRY")
        => CustomerDiscountRuleEntity.Create(
            customerPlanId: null, categoryCode: null, currencyCode: currency,
            discountType: CustomerDiscountType.Percent, discountRate: 0.05m, fixedDiscountAmount: null,
            minimumPurchaseAmount: null, maximumDiscountAmount: null,
            fundingMode: CustomerDiscountFundingMode.PlatformFunded, platformFundingRate: null, providerFundingRate: null,
            requiresProviderConsent: false, priority: CommissionRulePriority.Standard,
            effectiveFrom: from, effectiveTo: null, ruleCode: code);
}
