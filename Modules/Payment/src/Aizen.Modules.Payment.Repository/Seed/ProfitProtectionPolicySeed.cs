using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.ProfitProtection;
using Aizen.Modules.Payment.Repository.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Repository.Seed;

/// <summary>
/// Seeds one Active default ProfitProtectionPolicy (TRY) (BE-P5, §8). <b>All values below are documented PLACEHOLDERS,
/// admin-tunable</b> — the engine bakes in no constant; final numbers come from admin/YMM. Idempotent + self-healing
/// for the seed-owned policy (identified by PolicyCode IS NULL + currency TRY).
/// </summary>
public sealed class ProfitProtectionPolicySeed
{
    private readonly PaymentDbContext _db;
    private readonly ILogger<ProfitProtectionPolicySeed> _logger;

    public ProfitProtectionPolicySeed(PaymentDbContext db, ILogger<ProfitProtectionPolicySeed> logger)
    {
        _db = db;
        _logger = logger;
    }

    private const string Currency = "TRY";

    // ── Placeholder defaults (admin-tunable; NOT business constants) ─────────────
    private const decimal MinCustomerAmt = 0m,  MinCustomerRate = 0m;
    private const decimal MinProviderAmt = 0m,  MinProviderRate = 0m;
    private const decimal MinTxnAmt      = 10m, MinTxnRate      = 0.01m;
    private const decimal ProcessingRate = 0.029m, ProcessingFixed = 0.25m;   // e.g. gateway ~2.9% + fixed
    private const decimal RefundReserveRate = 0.005m;
    private const decimal OtherRate = 0m, OtherFixed = 0m;
    private const decimal CustomerVarShare = 0.50m;

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var seed = await _db.ProfitProtectionPolicies
            .FirstOrDefaultAsync(x => x.PolicyCode == null && x.CurrencyCode == Currency, ct);

        if (seed is not null)
        {
            _logger.LogDebug("ProfitProtectionPolicySeed skipped — default TRY policy already present.");
            return;
        }

        var policy = ProfitProtectionPolicyEntity.Create(
            currencyCode:                       Currency,
            minCustomerSideAmount:              MinCustomerAmt, minCustomerSideRate: MinCustomerRate,
            minProviderSideAmount:              MinProviderAmt, minProviderSideRate: MinProviderRate,
            minTransactionAmount:               MinTxnAmt,      minTransactionRate:  MinTxnRate,
            paymentProcessingExpenseRate:       ProcessingRate, paymentProcessingFixed: ProcessingFixed,
            refundRiskReserveRate:              RefundReserveRate,
            otherVariableExpenseRate:           OtherRate,      otherVariableExpenseFixed: OtherFixed,
            customerSideVariableCostShareRate:  CustomerVarShare,
            adjustmentOrder:                    ProfitProtectionAdjustmentOrder.PlatformDiscountThenCommissionBenefit,
            effectiveFrom:                      DateTime.UtcNow,
            effectiveTo:                        null,
            policyCode:                         null,
            policyName:                         "Default profit-protection policy (placeholder)",
            notes:                              "Launch placeholder — all thresholds admin-tunable; final values pending admin/YMM.");

        await _db.ProfitProtectionPolicies.AddAsync(policy, ct);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Seeding default profit-protection policy (TRY, placeholder thresholds).");
    }
}
