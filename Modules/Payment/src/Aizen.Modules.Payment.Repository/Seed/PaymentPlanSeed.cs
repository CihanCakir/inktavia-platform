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
    private async Task SeedCommissionRulesAsync(CancellationToken ct)
    {
        // ── Global fallback: 15% (BE-P2 authoritative, admin-tunable) ─────────
        // Self-healing for the seed-OWNED global (RuleCode IS NULL): create if missing, else reconcile the
        // rate/priority to the P2 target. Admin- and mock-created rules (which carry a RuleCode) are untouched.
        var seedGlobal = await _db.CommissionRules
            .FirstOrDefaultAsync(x => x.RuleType == CommissionRuleType.Global && x.RuleCode == null, ct);

        if (seedGlobal is null)
        {
            await _db.CommissionRules.AddAsync(CommissionRuleEntity.CreateGlobal(0.15m,
                DateTime.UtcNow, null, CommissionRulePriority.Standard,
                notes: "MVP global fallback — applies when no Plan/Category/Override rule matches",
                ruleCode: null), ct);
            _logger.LogInformation("Seeding global commission rule: 15%");
        }
        else if (seedGlobal.CommissionRate != 0.15m || seedGlobal.Priority != CommissionRulePriority.Standard)
        {
            seedGlobal.Update(0.15m, seedGlobal.EffectiveFrom, seedGlobal.EffectiveTo,
                CommissionRulePriority.Standard, seedGlobal.Notes);
            _logger.LogInformation("Reconciling seed global commission rule to 15% / Standard.");
        }

        // ── Category overrides ────────────────────────────────────────────────
        // These complement Plan rules — Category beats Global but loses to Plan/Override.
        var categoryRules = new[]
        {
            ("ENGINE_MAINTENANCE", 0.12m),
            ("ANTIFOULING",        0.10m),
            ("GENERAL_CLEANING",   0.18m),
            ("CARGODRY_RENEWAL",   0.05m),  // CargoDry renewals collected by platform — low rate
        };

        foreach (var (code, rate) in categoryRules)
        {
            if (!await _db.CommissionRules.AnyAsync(
                    x => x.RuleType == CommissionRuleType.Category && x.CategoryCode == code, ct))
            {
                await _db.CommissionRules.AddAsync(
                    CommissionRuleEntity.CreateForCategory(code, rate,
                        DateTime.UtcNow, null, CommissionRulePriority.Standard,
                        notes: $"MVP category default for {code}",
                        ruleCode: null), ct);
                _logger.LogInformation("Seeding category commission rule: {Code} = {Rate:P0}", code, rate);
            }
        }

        // ── Plan-level rules (BE-P2: FREE 15% / STANDARD 12% / PREMIUM_PARTNER 9%) ──
        // Resolved from DB now that Phase 1 SaveChanges has run and PKs are assigned.
        // Self-healing for the seed-OWNED plan rule (RuleCode IS NULL): create if missing, else reconcile
        // the rate to the P2 target. Admin/mock rules (with a RuleCode) are untouched.
        var planRates = new Dictionary<string, decimal>
        {
            { "FREE",            0.15m },
            { "STANDARD",        0.12m },
            { "PREMIUM_PARTNER", 0.09m },
        };

        foreach (var (planCode, rate) in planRates)
        {
            var plan = await _db.ProviderPlans
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.PlanCode == planCode, ct);

            if (plan is null)
            {
                _logger.LogWarning(
                    "SeedCommissionRules: ProviderPlan '{Code}' not found — skipping plan-level rule.", planCode);
                continue;
            }

            var seedRule = await _db.CommissionRules.FirstOrDefaultAsync(
                x => x.RuleType == CommissionRuleType.Plan && x.ProviderPlanId == plan.Id && x.RuleCode == null, ct);

            if (seedRule is null)
            {
                await _db.CommissionRules.AddAsync(
                    CommissionRuleEntity.CreateForPlan(plan.Id, rate,
                        DateTime.UtcNow, null, CommissionRulePriority.Standard,
                        notes: $"Plan-level commission for {planCode} tier",
                        ruleCode: null), ct);
                _logger.LogInformation(
                    "Seeding plan commission rule: {Code} (Id={Id}) = {Rate:P0}", planCode, plan.Id, rate);
            }
            else if (seedRule.CommissionRate != rate || seedRule.Priority != CommissionRulePriority.Standard)
            {
                seedRule.Update(rate, seedRule.EffectiveFrom, seedRule.EffectiveTo,
                    CommissionRulePriority.Standard, seedRule.Notes);
                _logger.LogInformation(
                    "Reconciling plan commission rule: {Code} (Id={Id}) → {Rate:P0} / Standard.",
                    planCode, plan.Id, rate);
            }
        }
    }
}
