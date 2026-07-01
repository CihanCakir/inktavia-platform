using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.Commission;
using Aizen.Modules.Payment.Repository.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Repository.Seed;

/// <summary>
/// Seeds 5 rich mock commission rules to match the frontend CommissionRuleDetailPage
/// mock data exactly (rule codes: CR-2024-X91, CR-2024-M04, CR-2023-B88, CR-2024-D12, CR-2024-K09).
///
/// Idempotent — each rule is skipped if a rule with the same RuleCode already exists.
/// Must run AFTER PaymentPlanSeed (which seeds plans and base rules).
/// </summary>
public sealed class CommissionRuleMockSeed
{
    private readonly PaymentDbContext                 _db;
    private readonly ILogger<CommissionRuleMockSeed>  _logger;

    public CommissionRuleMockSeed(PaymentDbContext db, ILogger<CommissionRuleMockSeed> logger)
    {
        _db     = db;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var existing = await _db.CommissionRules
            .Where(r => r.RuleCode != null)
            .Select(r => r.RuleCode!)
            .ToListAsync(ct);

        var existingSet = new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase);

        var rules = BuildRules(existingSet);
        if (rules.Count == 0)
        {
            _logger.LogDebug("CommissionRuleMockSeed skipped — all mock rules already present.");
            return;
        }

        await _db.CommissionRules.AddRangeAsync(rules, ct);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("CommissionRuleMockSeed: {Count} rule(s) seeded.", rules.Count);
    }

    private static List<CommissionRuleEntity> BuildRules(HashSet<string> existingCodes)
    {
        var list = new List<CommissionRuleEntity>();
        var now  = DateTime.UtcNow;

        // ── CR-2024-X91 — Global Active (standard base rate) ─────────────────
        if (!existingCodes.Contains("CR-2024-X91"))
            list.Add(CommissionRuleEntity.CreateGlobal(
                rate:          0.12m,
                effectiveFrom: new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                effectiveTo:   null,
                priority:      CommissionRulePriority.Standard,
                notes:         "Platform-wide base commission rate. Applies to all transactions not covered by a more specific rule.",
                ruleCode:      "CR-2024-X91"));

        // ── CR-2024-M04 — Plan rule (Marina Pro plan override) ───────────────
        // ProviderPlanId 1 = STANDARD plan (seeded by PaymentPlanSeed)
        if (!existingCodes.Contains("CR-2024-M04"))
            list.Add(CommissionRuleEntity.CreateForPlan(
                providerPlanId: 1,
                rate:           0.08m,
                effectiveFrom:  new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc),
                effectiveTo:    new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc),
                priority:       CommissionRulePriority.Medium,
                notes:          "Reduced rate for providers on the Standard plan. Negotiated Q1 2024.",
                ruleCode:       "CR-2024-M04"));

        // ── CR-2023-B88 — Category rule (Hull & Maintenance) ─────────────────
        if (!existingCodes.Contains("CR-2023-B88"))
            list.Add(CommissionRuleEntity.CreateForCategory(
                categoryCode:  "HULL_MAINTENANCE",
                rate:          0.10m,
                effectiveFrom: new DateTime(2023, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                effectiveTo:   null,
                priority:      CommissionRulePriority.Standard,
                notes:         "Category-level override for hull and maintenance services. Competitive rate to attract specialized providers.",
                ruleCode:      "CR-2023-B88"));

        // ── CR-2024-D12 — ProviderOverride rule (VIP provider negotiated rate)
        // ProviderProfileId 11011 = Marina Ops (from identity-profiles seed)
        if (!existingCodes.Contains("CR-2024-D12"))
            list.Add(CommissionRuleEntity.CreateProviderOverride(
                providerProfileId: 11011,
                rate:              0.05m,
                effectiveFrom:     new DateTime(2024, 5, 15, 0, 0, 0, DateTimeKind.Utc),
                effectiveTo:       new DateTime(2025, 5, 15, 0, 0, 0, DateTimeKind.Utc),
                priority:          CommissionRulePriority.High,
                notes:             "Individually negotiated rate for Marina Operations — top-tier provider by volume. Annual review scheduled May 2025.",
                ruleCode:          "CR-2024-D12"));

        // ── CR-2024-K09 — Global Emergency override (temporary surge rate) ────
        if (!existingCodes.Contains("CR-2024-K09"))
        {
            var emergencyRule = CommissionRuleEntity.CreateGlobal(
                rate:          0.18m,
                effectiveFrom: new DateTime(2024, 8, 1, 0, 0, 0, DateTimeKind.Utc),
                effectiveTo:   new DateTime(2024, 9, 30, 0, 0, 0, DateTimeKind.Utc),
                priority:      CommissionRulePriority.EMERGENCY,
                notes:         "Peak-season emergency rate applied for August–September 2024. Now expired.",
                ruleCode:      "CR-2024-K09");
            // Already expired — status derived automatically by DeriveStatus
            list.Add(emergencyRule);
        }

        return list;
    }
}
