using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.Plan;
using Aizen.Modules.Payment.Repository.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Repository.Seed;

/// <summary>
/// Seeds the global, contiguous ProviderPlanPrice chain (BE-P4, §6/§13.2). Launch = the GLOBAL go-live campaign
/// window [go-live, go-live+6mo); List = [go-live+6mo, ∞). Per plan (Monthly, TRY):
///   STANDARD 499 → 1490, PREMIUM_PARTNER 999 → 3490, FREE single open 0.
/// launch.EffectiveTo == list.EffectiveFrom (no gap/overlap). Idempotent (skip if same-scope+range exists).
/// go-live is a single configurable calendar date (Payment:GoLiveDateUtc), NOT per-provider. Values are
/// launch defaults, admin-tunable.
/// </summary>
public sealed class ProviderPlanPriceSeed
{
    private readonly PaymentDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<ProviderPlanPriceSeed> _logger;

    public ProviderPlanPriceSeed(PaymentDbContext db, IConfiguration config, ILogger<ProviderPlanPriceSeed> logger)
    {
        _db = db;
        _config = config;
        _logger = logger;
    }

    // Fixed default go-live (V1.0.1 launch). Global calendar window — the current instant (2026-07) falls in Launch.
    private static readonly DateTime DefaultGoLive = new(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);
    private const string Currency = "TRY";

    private DateTime ResolveGoLiveUtc()
    {
        var raw = _config["Payment:GoLiveDateUtc"];
        if (!string.IsNullOrWhiteSpace(raw) &&
            DateTime.TryParse(raw, null, System.Globalization.DateTimeStyles.AdjustToUniversal, out var parsed))
            return DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
        return DefaultGoLive;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var goLive    = ResolveGoLiveUtc();
        var launchEnd = goLive.AddMonths(6);

        // (planCode, launchPrice, listPrice) — FREE handled as a single open row below.
        var paidPlans = new[]
        {
            ("STANDARD",        499m,  1490m),
            ("PREMIUM_PARTNER", 999m,  3490m),
        };

        var added = 0;

        foreach (var (planCode, launchPrice, listPrice) in paidPlans)
        {
            var plan = await _db.ProviderPlans.AsNoTracking().FirstOrDefaultAsync(x => x.PlanCode == planCode, ct);
            if (plan is null)
            {
                _logger.LogWarning("ProviderPlanPriceSeed: plan '{Code}' not found — skipping.", planCode);
                continue;
            }

            added += await EnsureRowAsync(plan.Id, ProviderPlanPriceType.Launch, launchPrice, goLive, launchEnd, ct);
            added += await EnsureRowAsync(plan.Id, ProviderPlanPriceType.List,   listPrice,  launchEnd, null,    ct);
        }

        // FREE — single open row at 0.
        var free = await _db.ProviderPlans.AsNoTracking().FirstOrDefaultAsync(x => x.PlanCode == "FREE", ct);
        if (free is not null)
            added += await EnsureRowAsync(free.Id, ProviderPlanPriceType.List, 0m, goLive, null, ct);

        if (added > 0)
        {
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("ProviderPlanPriceSeed: {Count} price row(s) seeded (go-live {GoLive:yyyy-MM-dd}).",
                added, goLive);
        }
    }

    private async Task<int> EnsureRowAsync(
        long planId, ProviderPlanPriceType type, decimal price, DateTime from, DateTime? to, CancellationToken ct)
    {
        var exists = await _db.ProviderPlanPrices.AnyAsync(x =>
            x.ProviderPlanId == planId && x.CurrencyCode == Currency && x.BillingPeriod == BillingPeriod.Monthly
            && x.EffectiveFrom == from && x.EffectiveTo == to, ct);

        if (exists) return 0;

        var row = ProviderPlanPriceEntity.Create(
            planId, type, BillingPeriod.Monthly, price, Currency, from, to,
            priceCode: null, notes: $"Seed default ({type}) — admin-tunable.");
        await _db.ProviderPlanPrices.AddAsync(row, ct);
        return 1;
    }
}
