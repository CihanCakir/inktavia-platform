using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.CustomerBenefit;
using Aizen.Modules.Payment.Domain.Entities.CustomerDiscount;
using Aizen.Modules.Payment.Repository.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Repository.Seed;

/// <summary>
/// BE-P6 reconciliation seed. (1) Turns each participant plan's legacy <c>ServiceDiscountRate</c> into the authoritative
/// <see cref="CustomerDiscountRuleEntity"/> — a PlatformFunded, plan-scoped Percent rule (legacy plan fields kept for
/// display). (2) Seeds one default <see cref="CustomerBenefitBudgetPolicyEntity"/> per plan. Idempotent + self-healing;
/// budget rate is a documented placeholder (admin-tunable). Runs after PaymentPlanSeed (participant plan PKs must exist).
/// </summary>
public sealed class CustomerDiscountBenefitSeed
{
    private readonly PaymentDbContext _db;
    private readonly ILogger<CustomerDiscountBenefitSeed> _logger;

    public CustomerDiscountBenefitSeed(PaymentDbContext db, ILogger<CustomerDiscountBenefitSeed> logger)
    {
        _db = db;
        _logger = logger;
    }

    private const string Currency = "TRY";
    private const decimal DefaultBenefitBudgetRate = 0.10m;   // placeholder — admin-tunable (ELITE ≠ unlimited)

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var plans = await _db.ParticipantPlans.AsNoTracking().ToListAsync(ct);
        var added = 0;

        foreach (var plan in plans)
        {
            // (1) Discount rule from the legacy ServiceDiscountRate (only when there is a discount to grant).
            if (plan.ServiceDiscountRate > 0m)
            {
                var exists = await _db.CustomerDiscountRules.AnyAsync(x =>
                    x.CustomerPlanId == plan.Id && x.CurrencyCode == Currency && x.CategoryCode == null
                    && x.FundingMode == CustomerDiscountFundingMode.PlatformFunded, ct);

                if (!exists)
                {
                    await _db.CustomerDiscountRules.AddAsync(CustomerDiscountRuleEntity.Create(
                        customerPlanId:          plan.Id,
                        categoryCode:            null,
                        currencyCode:            Currency,
                        discountType:            CustomerDiscountType.Percent,
                        discountRate:            plan.ServiceDiscountRate,
                        fixedDiscountAmount:     null,
                        minimumPurchaseAmount:   null,
                        maximumDiscountAmount:   null,
                        fundingMode:             CustomerDiscountFundingMode.PlatformFunded,
                        platformFundingRate:     null,
                        providerFundingRate:     null,
                        requiresProviderConsent: false,
                        priority:                CommissionRulePriority.Standard,
                        effectiveFrom:           DateTime.UtcNow,
                        effectiveTo:             null,
                        ruleCode:                null,
                        ruleName:                $"Plan service discount ({plan.PlanCode})",
                        notes:                   "Reconciled from ParticipantPlan.ServiceDiscountRate (legacy field kept for display)."), ct);
                    added++;
                    _logger.LogInformation("Seeding customer discount rule for plan {Code} = {Rate:P0} (PlatformFunded).",
                        plan.PlanCode, plan.ServiceDiscountRate);
                }
            }

            // (2) Default benefit budget policy per plan.
            var policyExists = await _db.CustomerBenefitBudgetPolicies.AnyAsync(x =>
                x.CustomerPlanId == plan.Id && x.CurrencyCode == Currency, ct);
            if (!policyExists)
            {
                await _db.CustomerBenefitBudgetPolicies.AddAsync(CustomerBenefitBudgetPolicyEntity.Create(
                    customerPlanId:      plan.Id,
                    currencyCode:        Currency,
                    benefitBudgetRate:   DefaultBenefitBudgetRate,
                    perPeriodMax:        null,
                    perCategoryLimit:    null,
                    perTransactionLimit: null,
                    refundRestorePolicy: BenefitRefundRestorePolicy.Restore,
                    effectiveFrom:       DateTime.UtcNow,
                    effectiveTo:         null,
                    policyCode:          null,
                    notes:               "Launch placeholder benefit budget rate — admin-tunable (ELITE ≠ unlimited)."), ct);
                added++;
            }
        }

        if (added > 0)
        {
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("CustomerDiscountBenefitSeed: {Count} row(s) seeded.", added);
        }
    }
}
