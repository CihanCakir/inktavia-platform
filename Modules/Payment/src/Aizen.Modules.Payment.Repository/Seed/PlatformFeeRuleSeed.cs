using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.PlatformFee;
using Aizen.Modules.Payment.Repository.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Repository.Seed;

/// <summary>
/// Seeds the default Global platform fee rule (BE-P3, §6): PercentageWithBounds 2.5% / min ₺99 / max ₺1.500 (TRY).
/// These are launch defaults — admin-tunable, not hard constants. Idempotent + self-healing for the seed-OWNED
/// rule (identified by RuleCode IS NULL + Global scope): create if missing, else reconcile the parameters.
/// </summary>
public sealed class PlatformFeeRuleSeed
{
    private readonly PaymentDbContext _db;
    private readonly ILogger<PlatformFeeRuleSeed> _logger;

    public PlatformFeeRuleSeed(PaymentDbContext db, ILogger<PlatformFeeRuleSeed> logger)
    {
        _db = db;
        _logger = logger;
    }

    // Launch defaults (admin-tunable).
    private const decimal DefaultRate = 0.025m;
    private const decimal DefaultMin  = 99m;
    private const decimal DefaultMax  = 1500m;
    private const string  Currency    = "TRY";

    public async Task SeedAsync(CancellationToken ct = default)
    {
        // Seed-owned Global rule for TRY = RuleCode null + no CategoryCode + no CustomerType + currency TRY.
        var seedRule = await _db.PlatformFeeRules.FirstOrDefaultAsync(x =>
            x.RuleCode == null && x.CategoryCode == null && x.CustomerType == null && x.CurrencyCode == Currency, ct);

        if (seedRule is null)
        {
            var rule = PlatformFeeRuleEntity.Create(
                model:         PlatformFeeModel.PercentageWithBounds,
                rate:          DefaultRate,
                fixedAmount:   null,
                minAmount:     DefaultMin,
                maxAmount:     DefaultMax,
                currencyCode:  Currency,
                categoryCode:  null,
                customerType:  null,
                priority:      CommissionRulePriority.Standard,
                effectiveFrom: DateTime.UtcNow,
                effectiveTo:   null,
                ruleCode:      null,
                ruleName:      "Default platform fee (bounded 2.5%)",
                notes:         "Launch default — PercentageWithBounds 2.5% / min 99 / max 1500 TRY. Admin-tunable.");

            await _db.PlatformFeeRules.AddAsync(rule, ct);
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Seeding default platform fee rule: PercentageWithBounds 2.5% / 99 / 1500 TRY.");
        }
        else if (seedRule.Model != PlatformFeeModel.PercentageWithBounds
                 || seedRule.Rate != DefaultRate || seedRule.MinAmount != DefaultMin || seedRule.MaxAmount != DefaultMax
                 || seedRule.Priority != CommissionRulePriority.Standard)
        {
            seedRule.Update(
                PlatformFeeModel.PercentageWithBounds, DefaultRate, null, DefaultMin, DefaultMax,
                CommissionRulePriority.Standard, seedRule.EffectiveFrom, seedRule.EffectiveTo,
                seedRule.RuleName, seedRule.Notes, seedRule.VatRate);
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Reconciling default platform fee rule to PercentageWithBounds 2.5% / 99 / 1500 / Standard.");
        }
    }
}
