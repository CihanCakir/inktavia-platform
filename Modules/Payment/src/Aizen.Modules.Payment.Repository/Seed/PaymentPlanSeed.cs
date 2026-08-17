using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.Commission;
using Aizen.Modules.Payment.Domain.Entities.Plan;
using Aizen.Modules.Payment.Repository.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Repository.Seed;

/// <summary>
/// Seeds MVP default plans and commission rules.
/// All values are configurable via admin panel post-seed.
/// These are MVP defaults only — not final commercial decisions.
/// </summary>
public sealed class PaymentPlanSeed
{
    private readonly PaymentDbContext _db;
    private readonly ILogger<PaymentPlanSeed> _logger;

    public PaymentPlanSeed(PaymentDbContext db, ILogger<PaymentPlanSeed> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        // Phase 1: persist plans first so EF assigns real PKs before commission rules reference them
        await SeedProviderPlansAsync(ct);
        await SeedParticipantPlansAsync(ct);
        await _db.SaveChangesAsync(ct);

        // Phase 2: commission rules — plan PKs are now available in DB
        await SeedCommissionRulesAsync(ct);
        await _db.SaveChangesAsync(ct);
    }

    // ── Provider Plans ────────────────────────────────────────────────────────
    // MVP defaults. Commission rates here are for documentation only;
    // actual commission is resolved from CommissionRule (Plan type) at transaction time.
    private async Task SeedProviderPlansAsync(CancellationToken ct)
    {
        var plans = new[]
        {
            // FREE: ₺0/month, limited to 5 active offers at a time
            ProviderPlanEntity.Create("FREE", "Free", null, 0m, null, null, null, 5, false, false, 1),
            // STANDARD: ₺499/month, unlimited offers
            ProviderPlanEntity.Create("STANDARD", "Standard", null, 499m, null, null, null, null, false, true, 2),
            // PREMIUM_PARTNER: ₺999/month, priority boost, unlimited offers
            ProviderPlanEntity.Create("PREMIUM_PARTNER", "Premium Partner", null, 999m, null, null, null, null, true, true, 3),
        };

        foreach (var plan in plans)
        {
            if (!await _db.ProviderPlans.AnyAsync(x => x.PlanCode == plan.PlanCode, ct))
            {
                await _db.ProviderPlans.AddAsync(plan, ct);
                _logger.LogInformation("Seeding ProviderPlan: {Code}", plan.PlanCode);
            }
        }
    }

    // ── Participant Plans ─────────────────────────────────────────────────────
    private async Task SeedParticipantPlansAsync(CancellationToken ct)
    {
        var plans = new[]
        {
            // BASIC: ₺0/month, no discounts, 1.0x InkCoin earn
            ParticipantPlanEntity.Create("BASIC", "Basic", null, 0m, null, null, null, 0m, 0m, 1.0m, 1),
            // GOLD: ₺199/month, 5% service discount, 10% CargoDry discount, 1.5x earn
            ParticipantPlanEntity.Create("GOLD", "Gold", null, 199m, null, null, null, 0.05m, 0.10m, 1.5m, 2),
            // PLATINUM: ₺399/month, 10% service discount, 20% CargoDry discount, 2.0x earn
            ParticipantPlanEntity.Create("PLATINUM", "Platinum", null, 399m, null, null, null, 0.10m, 0.20m, 2.0m, 3),
        };

        foreach (var plan in plans)
        {
            if (!await _db.ParticipantPlans.AnyAsync(x => x.PlanCode == plan.PlanCode, ct))
            {
                await _db.ParticipantPlans.AddAsync(plan, ct);
                _logger.LogInformation("Seeding ParticipantPlan: {Code}", plan.PlanCode);
            }
        }
    }

    // ── Commission Rules ──────────────────────────────────────────────────────
    // Precedence chain: ProviderOverride > Plan > Category > Global
    // Called AFTER SeedProviderPlansAsync + SaveChangesAsync so plan PKs are available.
    //
    // Every base rule carries a STABLE, UNIQUE RuleCode. This is REQUIRED: the economics line-commission gate rejects a
    // resolved-but-code-less rule (ServiceRequestEconomicsCommissionUnresolved), so a null-code base rule breaks accept.
    // Idempotency/duplicate-safety is keyed on the RuleCode (natural key), NOT on "RuleCode IS NULL" — a legacy null-code
    // seed row is ADOPTED (backfilled) rather than duplicated, so re-seeds/restarts insert nothing and never trip the
    // fail-loud CommissionRuleConflict guard. Rates are the BE-P2 targets (unchanged).
    private const string GlobalRuleCode = "CR-GLOBAL-DEFAULT";

    private async Task SeedCommissionRulesAsync(CancellationToken ct)
    {
        // ── Global fallback: 15% (BE-P2 authoritative, admin-tunable) ─────────
        await UpsertSeedRuleAsync(
            ruleCode:     GlobalRuleCode,
            legacyMatch:  x => x.RuleType == CommissionRuleType.Global && x.RuleCode == null,
            factory:      () => CommissionRuleEntity.CreateGlobal(0.15m,
                              DateTime.UtcNow, null, CommissionRulePriority.Standard,
                              notes: "MVP global fallback — applies when no Plan/Category/Override rule matches",
                              ruleCode: GlobalRuleCode),
            targetRate:   0.15m, ct);

        // ── Category overrides ────────────────────────────────────────────────
        // These complement Plan rules — Category beats Global but loses to Plan/Override.
        // RuleCode is varchar(20) — the short codes below stay within that (a full "CR-CAT-{category}" would overflow).
        var categoryRules = new[]
        {
            ("ENGINE_MAINTENANCE", 0.12m, "CR-CAT-ENGINE"),
            ("ANTIFOULING",        0.10m, "CR-CAT-ANTIFOUL"),
            ("GENERAL_CLEANING",   0.18m, "CR-CAT-CLEAN"),
            ("CARGODRY_RENEWAL",   0.05m, "CR-CAT-CARGODRY"),  // CargoDry renewals collected by platform — low rate
        };

        foreach (var (code, rate, ruleCode) in categoryRules)
        {
            var categoryCode = code;
            await UpsertSeedRuleAsync(
                ruleCode:    ruleCode,
                legacyMatch: x => x.RuleType == CommissionRuleType.Category && x.CategoryCode == categoryCode && x.RuleCode == null,
                factory:     () => CommissionRuleEntity.CreateForCategory(categoryCode, rate,
                                 DateTime.UtcNow, null, CommissionRulePriority.Standard,
                                 notes: $"MVP category default for {categoryCode}",
                                 ruleCode: ruleCode),
                targetRate:  rate, ct);
        }

        // ── Plan-level rules (BE-P2: FREE 15% / STANDARD 12% / PREMIUM_PARTNER 9%) ──
        // Resolved from DB now that Phase 1 SaveChanges has run and PKs are assigned.
        // (rate, short RuleCode ≤ varchar(20)) per plan.
        var planRates = new Dictionary<string, (decimal Rate, string RuleCode)>
        {
            { "FREE",            (0.15m, "CR-PLAN-FREE") },
            { "STANDARD",        (0.12m, "CR-PLAN-STD") },
            { "PREMIUM_PARTNER", (0.09m, "CR-PLAN-PREMIUM") },
        };

        foreach (var (planCode, spec) in planRates)
        {
            var rate = spec.Rate;
            var plan = await _db.ProviderPlans
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.PlanCode == planCode, ct);

            if (plan is null)
            {
                _logger.LogWarning(
                    "SeedCommissionRules: ProviderPlan '{Code}' not found — skipping plan-level rule.", planCode);
                continue;
            }

            var planId   = plan.Id;
            var ruleCode = spec.RuleCode;
            await UpsertSeedRuleAsync(
                ruleCode:    ruleCode,
                legacyMatch: x => x.RuleType == CommissionRuleType.Plan && x.ProviderPlanId == planId && x.RuleCode == null,
                factory:     () => CommissionRuleEntity.CreateForPlan(planId, rate,
                                 DateTime.UtcNow, null, CommissionRulePriority.Standard,
                                 notes: $"Plan-level commission for {planCode} tier",
                                 ruleCode: ruleCode),
                targetRate:  rate, ct);
        }
    }

    /// <summary>
    /// Duplicate-safe upsert for a seed-owned commission rule keyed on <paramref name="ruleCode"/>:
    /// (1) already present by code → reconcile rate/priority to the target if drifted;
    /// (2) legacy null-code seed row of the same scope → ADOPT it (backfill the code) + reconcile — never a duplicate;
    /// (3) neither → create fresh with the code. Guarantees restart/re-seed inserts nothing and cannot trip
    /// <c>CommissionRuleConflict</c>.
    /// </summary>
    private async Task UpsertSeedRuleAsync(
        string ruleCode,
        System.Linq.Expressions.Expression<Func<CommissionRuleEntity, bool>> legacyMatch,
        Func<CommissionRuleEntity> factory,
        decimal targetRate,
        CancellationToken ct)
    {
        var byCode = await _db.CommissionRules.FirstOrDefaultAsync(x => x.RuleCode == ruleCode, ct);
        if (byCode is not null)
        {
            if (byCode.CommissionRate != targetRate || byCode.Priority != CommissionRulePriority.Standard)
            {
                byCode.Update(targetRate, byCode.EffectiveFrom, byCode.EffectiveTo,
                    CommissionRulePriority.Standard, byCode.Notes);
                _logger.LogInformation("Reconciling seed commission rule {Code} → {Rate:P0} / Standard.", ruleCode, targetRate);
            }
            return;
        }

        var legacy = await _db.CommissionRules.FirstOrDefaultAsync(legacyMatch, ct);
        if (legacy is not null)
        {
            legacy.AssignRuleCode(ruleCode);
            if (legacy.CommissionRate != targetRate || legacy.Priority != CommissionRulePriority.Standard)
                legacy.Update(targetRate, legacy.EffectiveFrom, legacy.EffectiveTo, CommissionRulePriority.Standard, legacy.Notes);
            _logger.LogInformation("Adopted legacy null-code commission rule → {Code} (backfilled).", ruleCode);
            return;
        }

        await _db.CommissionRules.AddAsync(factory(), ct);
        _logger.LogInformation("Seeding commission rule {Code} = {Rate:P0}.", ruleCode, targetRate);
    }
}
