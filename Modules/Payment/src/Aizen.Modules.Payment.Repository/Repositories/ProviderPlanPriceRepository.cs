using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.Plan;
using Aizen.Modules.Payment.Domain.Entities.Subscription;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Aizen.Modules.Payment.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Payment.Repository.Repositories;

/// <summary>
/// Persistence + pure resolution for <see cref="ProviderPlanPriceEntity"/> (BE-P4). Resolution loads the scoped
/// active rows and delegates the point-in-time / overlap-gap logic to the domain
/// <see cref="ProviderPlanPriceResolver"/> — no writes on the resolve path.
/// </summary>
public sealed class ProviderPlanPriceRepository : IProviderPlanPriceRepository
{
    private readonly PaymentDbContext _db;
    public ProviderPlanPriceRepository(PaymentDbContext db) => _db = db;

    // ── Resolution (pure) ───────────────────────────────────────────────────────

    public async Task<ProviderPlanPriceEntity?> ResolveAsync(
        long planId, string currency, BillingPeriod billing, DateTime atUtc, CancellationToken ct = default)
    {
        var cur = currency.ToUpperInvariant();
        var scoped = await _db.ProviderPlanPrices
            .AsNoTracking()
            .Where(x => x.ProviderPlanId == planId
                     && x.CurrencyCode   == cur
                     && x.BillingPeriod  == billing
                     && x.IsActive)
            .ToListAsync(ct);

        return ProviderPlanPriceResolver.Resolve(scoped, atUtc);
    }

    public Task<ProviderPlanPriceEntity?> ResolveRenewalPriceAsync(
        ProviderPlanSubscriptionEntity subscription, DateTime atRenewalUtc, CancellationToken ct = default)
        => ResolveAsync(subscription.ProviderPlanId, subscription.CurrencyCode, BillingPeriod.Monthly, atRenewalUtc, ct);

    public async Task<PlanPriceGuardResult> ValidateInsertableAsync(
        ProviderPlanPriceEntity candidate, CancellationToken ct = default)
    {
        var existing = await _db.ProviderPlanPrices
            .AsNoTracking()
            .Where(x => x.ProviderPlanId == candidate.ProviderPlanId
                     && x.CurrencyCode   == candidate.CurrencyCode
                     && x.BillingPeriod  == candidate.BillingPeriod
                     && x.IsActive)
            .ToListAsync(ct);

        return ProviderPlanPriceResolver.ValidateInsertable(candidate, existing);
    }

    // ── Read ────────────────────────────────────────────────────────────────────

    public Task<ProviderPlanPriceEntity?> GetByIdAsync(long id, CancellationToken ct = default)
        => _db.ProviderPlanPrices.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<List<ProviderPlanPriceEntity>> GetByPlanAsync(long planId, CancellationToken ct = default)
        => _db.ProviderPlanPrices
            .Where(x => x.ProviderPlanId == planId)
            .OrderBy(x => x.BillingPeriod).ThenBy(x => x.EffectiveFrom)
            .ToListAsync(ct);

    // ── Write ─────────────────────────────────────────────────────────────────

    public Task AddAsync(ProviderPlanPriceEntity entity, CancellationToken ct = default)
        => _db.ProviderPlanPrices.AddAsync(entity, ct).AsTask();

    public void Update(ProviderPlanPriceEntity entity) => _db.ProviderPlanPrices.Update(entity);
    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);

    public Task<bool> ExistsSameRangeAsync(
        long planId, string currency, BillingPeriod billing, DateTime effectiveFrom, DateTime? effectiveTo,
        CancellationToken ct = default)
    {
        var cur = currency.ToUpperInvariant();
        return _db.ProviderPlanPrices.AnyAsync(x =>
            x.ProviderPlanId == planId && x.CurrencyCode == cur && x.BillingPeriod == billing
            && x.EffectiveFrom == effectiveFrom && x.EffectiveTo == effectiveTo, ct);
    }

    public async Task<string> GenerateCodeAsync(CancellationToken ct = default)
    {
        var year  = DateTime.UtcNow.Year;
        var count = await _db.ProviderPlanPrices.CountAsync(ct);
        var seq   = (count + 1).ToString("D3");
        var suffix = ((char)('A' + (count % 26))).ToString()
                   + ((char)('A' + (count / 26 % 26))).ToString();
        return $"PPP-{year}-{suffix}{seq}";
    }
}
