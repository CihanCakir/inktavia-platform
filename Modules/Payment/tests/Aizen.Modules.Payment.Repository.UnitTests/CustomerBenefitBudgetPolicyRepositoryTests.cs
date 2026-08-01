using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.CustomerBenefit;
using Aizen.Modules.Payment.Repository.Persistence;
using Aizen.Modules.Payment.Repository.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Payment.Repository.UnitTests;

/// <summary>
/// Repository-level coverage for the BE-P6 admin list / detail surface of the per-plan customer-benefit budget policy
/// (create + list + view MVP — no update/deactivate/reactivate in the module today).
/// </summary>
public sealed class CustomerBenefitBudgetPolicyRepositoryTests
{
    private static PaymentDbContext NewDb()
        => new(new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase($"cbp-{Guid.NewGuid():N}").Options);

    [Fact]
    public async Task GetByIdAsync_Returns_Policy_And_Null_When_Missing()
    {
        await using var db = NewDb();
        var p = MakePolicy(planId: 1, from: DateTime.UtcNow.AddDays(-1), code: "CBP-DETAIL");
        db.CustomerBenefitBudgetPolicies.Add(p);
        await db.SaveChangesAsync();

        var repo = new CustomerBenefitBudgetPolicyRepository(db);

        (await repo.GetByIdAsync(p.Id))!.PolicyCode.Should().Be("CBP-DETAIL");
        (await repo.GetByIdAsync(999_999)).Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_Returns_Every_Policy_For_The_List()
    {
        await using var db = NewDb();
        db.CustomerBenefitBudgetPolicies.Add(MakePolicy(1, DateTime.UtcNow.AddDays(-10), "CBP-P1"));
        db.CustomerBenefitBudgetPolicies.Add(MakePolicy(2, DateTime.UtcNow.AddDays(-5),  "CBP-P2"));
        var inactive = MakePolicy(3, DateTime.UtcNow.AddDays(-20), "CBP-P3");
        inactive.Deactivate();
        db.CustomerBenefitBudgetPolicies.Add(inactive);
        await db.SaveChangesAsync();

        var repo = new CustomerBenefitBudgetPolicyRepository(db);
        var all = await repo.GetAllAsync();

        all.Should().HaveCount(3, "the list includes inactive/historical policies, not just the active ones");
    }

    private static CustomerBenefitBudgetPolicyEntity MakePolicy(long planId, DateTime from, string code, string currency = "TRY")
        => CustomerBenefitBudgetPolicyEntity.Create(
            customerPlanId: planId, currencyCode: currency, benefitBudgetRate: 0.10m,
            perPeriodMax: null, perCategoryLimit: null, perTransactionLimit: null,
            refundRestorePolicy: BenefitRefundRestorePolicy.Restore,
            effectiveFrom: from, effectiveTo: null, policyCode: code);
}
