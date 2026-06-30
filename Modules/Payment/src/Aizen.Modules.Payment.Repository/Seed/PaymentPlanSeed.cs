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
        await SeedProviderPlansAsync(ct);
        await SeedParticipantPlansAsync(ct);
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
            ProviderPlanEntity.Create("FREE", "Free", null, 0m, 5, false, false, 1),
            // STANDARD: ₺499/month, unlimited offers
            ProviderPlanEntity.Create("STANDARD", "Standard", null, 499m, null, false, true, 2),
            // PREMIUM_PARTNER: ₺999/month, priority boost, unlimited offers
            ProviderPlanEntity.Create("PREMIUM_PARTNER", "Premium Partner", null, 999m, null, true, true, 3),
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
            ParticipantPlanEntity.Create("BASIC", "Basic", null, 0m, 0m, 0m, 1.0m, 1),
            // GOLD: ₺199/month, 5% service discount, 10% CargoDry discount, 1.5x earn
            ParticipantPlanEntity.Create("GOLD", "Gold", null, 199m, 0.05m, 0.10m, 1.5m, 2),
            // PLATINUM: ₺399/month, 10% service discount, 20% CargoDry discount, 2.0x earn
            ParticipantPlanEntity.Create("PLATINUM", "Platinum", null, 399m, 0.10m, 0.20m, 2.0m, 3),
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
    // Plan IDs are resolved after plan seed — set via separate migration or admin panel.
    // MVP: seed Global + Category rules only. Plan-level rules are linked post-seed by admin.
    private async Task SeedCommissionRulesAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        // Global default: 15%
        if (!await _db.CommissionRules.AnyAsync(x => x.RuleType == CommissionRuleType.Global, ct))
        {
            await _db.CommissionRules.AddAsync(CommissionRuleEntity.CreateGlobal(0.15m), ct);
            _logger.LogInformation("Seeding global commission rule: 15%");
        }

        // Category overrides — MVP defaults
        var categoryRules = new[]
        {
            ("ENGINE_MAINTENANCE", 0.12m),
            ("ANTIFOULING",        0.10m),
            ("GENERAL_CLEANING",   0.18m),
        };

        foreach (var (code, rate) in categoryRules)
        {
            if (!await _db.CommissionRules.AnyAsync(
                    x => x.RuleType == CommissionRuleType.Category && x.CategoryCode == code, ct))
            {
                await _db.CommissionRules.AddAsync(
                    CommissionRuleEntity.CreateForCategory(code, rate), ct);
                _logger.LogInformation("Seeding category commission rule: {Code} = {Rate:P0}", code, rate);
            }
        }

        // Note: Plan-level commission rules (FREE=18%, STANDARD=12%, PREMIUM_PARTNER=8%)
        // are NOT seeded here because they require ProviderPlanEntity.Id foreign keys.
        // These must be created via the Admin Panel → Commission Rules screen after first run,
        // or added via a separate migration once plan IDs are known.
        // They will be resolved from the precedence chain — Global fallback applies until set.
    }
}
