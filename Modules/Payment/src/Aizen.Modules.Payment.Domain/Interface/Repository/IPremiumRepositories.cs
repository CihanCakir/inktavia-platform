using Aizen.Modules.Payment.Domain.Entities.Premium;

namespace Aizen.Modules.Payment.Domain.Interface.Repository;

/// <summary>BE-P11 §9.1 — premium product catalogue.</summary>
public interface IPremiumProductRepository
{
    Task<PremiumProductEntity?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<PremiumProductEntity?> GetByIdAsync(long id, CancellationToken ct = default);
    Task AddAsync(PremiumProductEntity entity, CancellationToken ct = default);
    void Update(PremiumProductEntity entity);
    Task SaveChangesAsync(CancellationToken ct = default);
}

/// <summary>BE-P11 §9.1/§13.9 — versioned premium price (BE-P4 resolver mirror).</summary>
public interface IPremiumProductPriceRepository
{
    /// <summary>Point-in-time resolution over the (product, currency) chain — delegates to <c>PremiumProductPriceResolver</c>.</summary>
    Task<PremiumProductPriceEntity?> ResolveAsync(long productId, string currency, DateTime atUtc, CancellationToken ct = default);
    Task<PremiumPriceGuardResult> ValidateInsertableAsync(PremiumProductPriceEntity candidate, CancellationToken ct = default);
    Task<PremiumProductPriceEntity?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<List<PremiumProductPriceEntity>> GetByProductAsync(long productId, CancellationToken ct = default);
    Task AddAsync(PremiumProductPriceEntity entity, CancellationToken ct = default);
    void Update(PremiumProductPriceEntity entity);
    Task SaveChangesAsync(CancellationToken ct = default);
}

/// <summary>BE-P11 §9.2 — premium purchases.</summary>
public interface IPremiumPurchaseRepository
{
    Task<PremiumPurchaseEntity?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<PremiumPurchaseEntity?> GetByPaymentTransactionIdAsync(long paymentTransactionId, CancellationToken ct = default);
    /// <summary>§9.2 duplicate guard — a Pending or Paid boost purchase already exists for (provider, offer, product).</summary>
    Task<PremiumPurchaseEntity?> GetActiveOrPendingAsync(long providerProfileId, long offerId, long productId, CancellationToken ct = default);
    Task AddAsync(PremiumPurchaseEntity entity, CancellationToken ct = default);
    void Update(PremiumPurchaseEntity entity);
    Task SaveChangesAsync(CancellationToken ct = default);
}

/// <summary>BE-P11 §9.2/§13.9 — premium entitlements (≤1 per purchase; expiration; read model).</summary>
public interface IPremiumEntitlementRepository
{
    Task<PremiumEntitlementEntity?> GetByPurchaseIdAsync(long premiumPurchaseId, CancellationToken ct = default);
    /// <summary>§13.9 read model — the currently-live Active entitlement for an offer (or null).</summary>
    Task<PremiumEntitlementEntity?> GetActiveByOfferAsync(long offerId, DateTime atUtc, CancellationToken ct = default);
    /// <summary>§13.9 — Active entitlements whose ExpiresAt is at/behind <paramref name="atUtc"/> (the expiration job).</summary>
    Task<List<PremiumEntitlementEntity>> GetExpirableAsync(DateTime atUtc, int max, CancellationToken ct = default);
    Task AddAsync(PremiumEntitlementEntity entity, CancellationToken ct = default);
    void Update(PremiumEntitlementEntity entity);
    Task SaveChangesAsync(CancellationToken ct = default);
}
