using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.Premium;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Aizen.Modules.Payment.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Payment.Repository.Repositories;

/// <summary>BE-P11 §9.1 — premium product catalogue.</summary>
public sealed class PremiumProductRepository : IPremiumProductRepository
{
    private readonly PaymentDbContext _db;
    public PremiumProductRepository(PaymentDbContext db) => _db = db;

    public Task<PremiumProductEntity?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        var c = code.Trim().ToUpperInvariant();
        return _db.PremiumProducts.FirstOrDefaultAsync(x => x.Code == c, ct);
    }

    public Task<PremiumProductEntity?> GetByIdAsync(long id, CancellationToken ct = default)
        => _db.PremiumProducts.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task AddAsync(PremiumProductEntity entity, CancellationToken ct = default)
        => _db.PremiumProducts.AddAsync(entity, ct).AsTask();

    public void Update(PremiumProductEntity entity) => _db.PremiumProducts.Update(entity);
    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}

/// <summary>BE-P11 §9.1/§13.9 — premium price resolution (pure resolver) + contiguity guard.</summary>
public sealed class PremiumProductPriceRepository : IPremiumProductPriceRepository
{
    private readonly PaymentDbContext _db;
    public PremiumProductPriceRepository(PaymentDbContext db) => _db = db;

    public async Task<PremiumProductPriceEntity?> ResolveAsync(
        long productId, string currency, DateTime atUtc, CancellationToken ct = default)
    {
        var cur = currency.ToUpperInvariant();
        var scoped = await _db.PremiumProductPrices
            .AsNoTracking()
            .Where(x => x.PremiumProductId == productId && x.CurrencyCode == cur && x.IsActive)
            .ToListAsync(ct);

        return PremiumProductPriceResolver.Resolve(scoped, atUtc);
    }

    public async Task<PremiumPriceGuardResult> ValidateInsertableAsync(
        PremiumProductPriceEntity candidate, CancellationToken ct = default)
    {
        var existing = await _db.PremiumProductPrices
            .AsNoTracking()
            .Where(x => x.PremiumProductId == candidate.PremiumProductId
                     && x.CurrencyCode == candidate.CurrencyCode && x.IsActive)
            .ToListAsync(ct);

        return PremiumProductPriceResolver.ValidateInsertable(candidate, existing);
    }

    public Task<PremiumProductPriceEntity?> GetByIdAsync(long id, CancellationToken ct = default)
        => _db.PremiumProductPrices.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<List<PremiumProductPriceEntity>> GetByProductAsync(long productId, CancellationToken ct = default)
        => _db.PremiumProductPrices
            .Where(x => x.PremiumProductId == productId)
            .OrderBy(x => x.EffectiveFrom)
            .ToListAsync(ct);

    public Task AddAsync(PremiumProductPriceEntity entity, CancellationToken ct = default)
        => _db.PremiumProductPrices.AddAsync(entity, ct).AsTask();

    public void Update(PremiumProductPriceEntity entity) => _db.PremiumProductPrices.Update(entity);
    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}

/// <summary>BE-P11 §9.2 — premium purchases.</summary>
public sealed class PremiumPurchaseRepository : IPremiumPurchaseRepository
{
    private readonly PaymentDbContext _db;
    public PremiumPurchaseRepository(PaymentDbContext db) => _db = db;

    public Task<PremiumPurchaseEntity?> GetByIdAsync(long id, CancellationToken ct = default)
        => _db.PremiumPurchases.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<PremiumPurchaseEntity?> GetByPaymentTransactionIdAsync(long paymentTransactionId, CancellationToken ct = default)
        => _db.PremiumPurchases.FirstOrDefaultAsync(x => x.PaymentTransactionId == paymentTransactionId, ct);

    public Task<PremiumPurchaseEntity?> GetActiveOrPendingAsync(
        long providerProfileId, long offerId, long productId, CancellationToken ct = default)
        => _db.PremiumPurchases.FirstOrDefaultAsync(x =>
            x.ProviderProfileId == providerProfileId && x.ContextRef == offerId && x.PremiumProductId == productId
            && (x.Status == PremiumPurchaseStatus.Pending || x.Status == PremiumPurchaseStatus.Paid), ct);

    public Task AddAsync(PremiumPurchaseEntity entity, CancellationToken ct = default)
        => _db.PremiumPurchases.AddAsync(entity, ct).AsTask();

    public void Update(PremiumPurchaseEntity entity) => _db.PremiumPurchases.Update(entity);
    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}

/// <summary>BE-P11 §9.2/§13.9 — premium entitlements.</summary>
public sealed class PremiumEntitlementRepository : IPremiumEntitlementRepository
{
    private readonly PaymentDbContext _db;
    public PremiumEntitlementRepository(PaymentDbContext db) => _db = db;

    public Task<PremiumEntitlementEntity?> GetByPurchaseIdAsync(long premiumPurchaseId, CancellationToken ct = default)
        => _db.PremiumEntitlements.FirstOrDefaultAsync(x => x.PremiumPurchaseId == premiumPurchaseId, ct);

    public Task<PremiumEntitlementEntity?> GetActiveByOfferAsync(long offerId, DateTime atUtc, CancellationToken ct = default)
        => _db.PremiumEntitlements
            .AsNoTracking()
            .Where(x => x.ContextRef == offerId && x.Status == PremiumEntitlementStatus.Active
                     && x.StartsAt <= atUtc && x.ExpiresAt > atUtc)
            .OrderByDescending(x => x.ExpiresAt)
            .FirstOrDefaultAsync(ct);

    public Task<List<PremiumEntitlementEntity>> GetExpirableAsync(DateTime atUtc, int max, CancellationToken ct = default)
        => _db.PremiumEntitlements
            .Where(x => x.Status == PremiumEntitlementStatus.Active && x.ExpiresAt != null && x.ExpiresAt <= atUtc)
            .OrderBy(x => x.ExpiresAt)
            .Take(max)
            .ToListAsync(ct);

    public Task AddAsync(PremiumEntitlementEntity entity, CancellationToken ct = default)
        => _db.PremiumEntitlements.AddAsync(entity, ct).AsTask();

    public void Update(PremiumEntitlementEntity entity) => _db.PremiumEntitlements.Update(entity);
    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
