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
    // Owner-decided commercial profit-protection value (not YMM-gated): the transaction revenue split is provider-heavy
    // (customer-side effective revenue ≈ 14–22% across plans, further lowered by the ₺1,500 platform-fee cap), so loading
    // 50% of variable cost onto the customer-side contribution is too aggressive; 0.25 leaves a small safety margin.
    // Admin-tunable at runtime (P5); RE-CALIBRATE after the first real settlement data (actual processing cost,
    // refund/chargeback rate, transaction mix).
    private const decimal CustomerVarShare = 0.25m;

    // ── BE-S9 line-level placeholders (§20.12; admin-tunable) — launch NO-OP so an offer whose lines already clear is
    //    byte-identical to pre-S9: no min-receivable floor, 100% funded-discount caps, 0 commission/contribution floor,
    //    strategic-loss exception OFF. Admin tightens these to turn the line gate on. ──
    private const decimal LineMinRecvRate = 0m,  LineMinRecvAmt = 0m;
    private const decimal AllowedProvFundedRate = 1m, AllowedPlatFundedRate = 1m;
    private const decimal LineCommFloorRate = 0m, MinLineContribRate = 0m;
    private const bool    StrategicLossEnabled = false;
    private const decimal StrategicLossMaxDeficit = 0m;

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
            notes:                              "Launch placeholder — all thresholds admin-tunable; final values pending admin/YMM.",
            // ── BE-S9 line-level placeholders (no-op) ──
            defaultLineMinProviderReceivableRate:    LineMinRecvRate,
            defaultLineMinProviderReceivableAmount:  LineMinRecvAmt,
            defaultAllowedProviderFundedDiscountRate: AllowedProvFundedRate,
            defaultAllowedPlatformFundedDiscountRate: AllowedPlatFundedRate,
            lineCommissionFloorRate:                 LineCommFloorRate,
            minLinePlatformContributionRate:         MinLineContribRate,
            strategicLossExceptionEnabled:           StrategicLossEnabled,
            strategicLossExceptionMaxLineDeficit:    StrategicLossMaxDeficit);

        await _db.ProfitProtectionPolicies.AddAsync(policy, ct);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Seeding default profit-protection policy (TRY, placeholder thresholds).");
    }
}
