using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.Premium;
using Aizen.Modules.Payment.Repository.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Repository.Seed;

/// <summary>
/// BE-P11 §9 — seeds the <c>OFFER_BOOST_7D</c> premium product (DurationDays 7, EntitlementType OfferBoost, Active) + one
/// open-ended active TRY price. Idempotent (keyed by product <c>Code</c>); the price amount is an <b>admin-tunable</b>
/// launch placeholder. Duplicate-seed-safe.
/// </summary>
public sealed class PremiumProductSeed
{
    private readonly PaymentDbContext _db;
    private readonly ILogger<PremiumProductSeed> _logger;
    public PremiumProductSeed(PaymentDbContext db, ILogger<PremiumProductSeed> logger) { _db = db; _logger = logger; }

    public const  string  OfferBoostCode  = "OFFER_BOOST_7D";
    private const string  Currency        = "TRY";
    private const int     DurationDays    = 7;
    private const decimal LaunchPrice     = 149.90m;   // admin-tunable launch placeholder

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var product = await _db.PremiumProducts.FirstOrDefaultAsync(x => x.Code == OfferBoostCode, ct);
        if (product is null)
        {
            product = PremiumProductEntity.Create(
                code: OfferBoostCode, name: "Offer Boost (7 days)",
                entitlementType: PremiumEntitlementType.OfferBoost, durationDays: DurationDays,
                status: PremiumProductStatus.Active,
                description: "Visibility/ranking boost for a single offer for 7 days (§19.4 — never affects commission).");
            await _db.PremiumProducts.AddAsync(product, ct);
            await _db.SaveChangesAsync(ct);   // materialise product.Id for the price FK
            _logger.LogInformation("Seeded premium product {Code}.", OfferBoostCode);
        }

        var hasPrice = await _db.PremiumProductPrices
            .AnyAsync(x => x.PremiumProductId == product.Id && x.CurrencyCode == Currency && x.IsActive, ct);
        if (!hasPrice)
        {
            var price = PremiumProductPriceEntity.Create(
                premiumProductId: product.Id, priceAmount: LaunchPrice, currencyCode: Currency,
                effectiveFrom: new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc), effectiveTo: null,
                priceCode: null,   // seed-owned; open-ended launch price
                notes: "Launch placeholder — admin-tunable.");
            await _db.PremiumProductPrices.AddAsync(price, ct);
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Seeded premium price for {Code} ({Price} {Cur}).", OfferBoostCode, LaunchPrice, Currency);
        }
        else
        {
            _logger.LogDebug("Premium price seed skipped (already present).");
        }
    }
}
