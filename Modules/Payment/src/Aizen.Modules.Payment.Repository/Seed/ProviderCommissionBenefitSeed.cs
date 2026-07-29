using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.CommissionBenefit;
using Aizen.Modules.Payment.Repository.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Repository.Seed;

/// <summary>
/// Seeds ONE <b>disabled example</b> provider commission-benefit rule (BE-P7, §9). Benefits are OFF by default — the
/// BE-P2 base rates stand until an admin explicitly enables a benefit. The example (COMMISSION_BENEFIT_1PP, −1pp,
/// stackable, min-rate floor) documents the shape only. Idempotent by RuleCode.
/// </summary>
public sealed class ProviderCommissionBenefitSeed
{
    private readonly PaymentDbContext _db;
    private readonly ILogger<ProviderCommissionBenefitSeed> _logger;

    public ProviderCommissionBenefitSeed(PaymentDbContext db, ILogger<ProviderCommissionBenefitSeed> logger)
    {
        _db = db;
        _logger = logger;
    }

    private const string ExampleCode = "PCB-EXAMPLE-1PP";

    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (await _db.ProviderCommissionBenefitRules.AnyAsync(x => x.RuleCode == ExampleCode, ct))
            return;

        var rule = ProviderCommissionBenefitRuleEntity.Create(
            ruleCode:                   ExampleCode,
            ruleName:                   "Example: COMMISSION_BENEFIT_1PP (disabled)",
            providerProfileId:          null,
            providerPlanId:             null,
            applicableCategoryCodes:    null,
            adjustmentPercentagePoints: -0.0100m,   // −1pp
            minimumCommissionRate:      0.0800m,     // rule-level floor
            maximumDiscountAmount:      null,
            maximumEligibleGmv:         null,
            usageLimit:                 null,
            stackable:                  true,
            exclusive:                  false,
            priority:                   CommissionRulePriority.Standard,
            effectiveFrom:              DateTime.UtcNow,
            effectiveTo:                null,
            currencyCode:               "TRY",
            notes:                      "Disabled example — admin-tunable. Benefits are OFF by default; enable to apply.");

        rule.Deactivate();   // OFF by default (Inactive → not a resolver candidate)

        await _db.ProviderCommissionBenefitRules.AddAsync(rule, ct);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Seeding disabled example provider commission benefit rule ({Code}).", ExampleCode);
    }
}
