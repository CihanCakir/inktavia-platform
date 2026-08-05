using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.PartCommercialTerm;
using Aizen.Modules.Payment.Repository.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Repository.Seed;

/// <summary>
/// BE-S5a — idempotent seed of a demonstrative part commercial term (category-scoped). Duplicate-seed protection: an
/// <c>AnyAsync</c> scope-key existence check before each insert, so re-running never double-seeds. Cost is real config here
/// (Payment-internal); it never leaves the module.
/// </summary>
public sealed class PartCommercialTermSeed
{
    private const string Currency = "TRY";

    private readonly PaymentDbContext _db;
    private readonly ILogger<PartCommercialTermSeed> _logger;

    public PartCommercialTermSeed(PaymentDbContext db, ILogger<PartCommercialTermSeed> logger)
    { _db = db; _logger = logger; }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var added = 0;

        // A category-scoped example for MAINTENANCE parts: ₺600 list, ₺120 dealer margin, up to ₺90 discountable,
        // funded 30 supplier / 40 provider / 20 platform (Σ90 ≤ 90), provider must still receive ≥ ₺500.
        added += await EnsureAsync(
            brand: null, productCode: null, providerProfileId: null, categoryCode: "MAINTENANCE",
            supplierListPrice: 600m, providerDealerMargin: 120m,
            maxCustomerDiscount: 90m, supplierFunded: 30m, providerFunded: 40m, platformFunded: 20m,
            minimumProviderReceivable: 500m, maximumDiscountableAmount: 90m,
            name: "MAINTENANCE parts — default commercial term", ct);

        if (added > 0)
        {
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("PartCommercialTermSeed: {Count} term(s) seeded.", added);
        }
    }

    private async Task<int> EnsureAsync(
        string? brand, string? productCode, long? providerProfileId, string? categoryCode,
        decimal supplierListPrice, decimal providerDealerMargin,
        decimal maxCustomerDiscount, decimal supplierFunded, decimal providerFunded, decimal platformFunded,
        decimal minimumProviderReceivable, decimal maximumDiscountableAmount, string name, CancellationToken ct)
    {
        var b = brand?.ToUpperInvariant();
        var p = productCode?.ToUpperInvariant();
        var c = categoryCode?.ToUpperInvariant();

        var exists = await _db.PartCommercialTerms.AnyAsync(x =>
            x.Brand == b && x.ProductCode == p && x.ProviderProfileId == providerProfileId
            && x.CategoryCode == c && x.CurrencyCode == Currency, ct);
        if (exists) return 0;

        await _db.PartCommercialTerms.AddAsync(PartCommercialTermEntity.Create(
            brand, productCode, providerProfileId, categoryCode, Currency,
            supplierListPrice, providerDealerMargin,
            maxCustomerDiscount, supplierFunded, providerFunded, platformFunded,
            minimumProviderReceivable, maximumDiscountableAmount,
            version: 1, priority: CommissionRulePriority.Standard,
            effectiveFrom: DateTime.UtcNow, effectiveTo: null, termCode: null, termName: name), ct);
        return 1;
    }
}
