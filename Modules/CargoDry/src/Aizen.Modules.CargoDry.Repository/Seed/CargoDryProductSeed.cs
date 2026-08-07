using Aizen.Modules.CargoDry.Domain.Entities;
using Aizen.Modules.CargoDry.Repository.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.CargoDry.Repository.Seed;

public sealed class CargoDryProductSeed
{
    private readonly CargoDryDbContext _db;
    private readonly ILogger<CargoDryProductSeed> _logger;

    public CargoDryProductSeed(CargoDryDbContext db, ILogger<CargoDryProductSeed> logger)
    {
        _db     = db;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var products = new[]
        {
            CargoDryProductEntity.Create("STANDARD-90",  "CargoDry Standard",
                "90-day moisture protection kit for standard marine storage.",
                90, 149.99m, "TRY"),
            CargoDryProductEntity.Create("PREMIUM-180", "CargoDry Premium",
                "180-day heavy-duty moisture control for yacht bilges and cabins.",
                180, 249.99m, "TRY"),
            CargoDryProductEntity.Create("PREMIUM-365", "CargoDry Premium Annual",
                "365-day comprehensive moisture management solution.",
                365, 399.99m, "TRY"),
            CargoDryProductEntity.Create("SMART-90", "CargoDry Smart",
                "90-day smart kit with IoT humidity sensor and real-time monitoring.",
                90, 299.99m, "TRY", hasSmartDevice: true),
        };

        foreach (var product in products)
        {
            var exists = await _db.Products.AnyAsync(
                x => x.ProductCode == product.ProductCode, ct);
            if (!exists)
            {
                await _db.Products.AddAsync(product, ct);
                _logger.LogInformation("Seeding CargoDry product: {Code}", product.ProductCode);
            }
        }

        await _db.SaveChangesAsync(ct);

        // Fill commercial pricing where missing (idempotent — only when ProviderCommissionRate is null)
        var pricing = new (string Code, decimal Consignment, decimal Rate)[]
        {
            ("STANDARD-90", 149.99m, 0.20m),
            ("PREMIUM-180", 249.99m, 0.22m),
            ("PREMIUM-365", 399.99m, 0.25m),
            ("SMART-90",    299.99m, 0.28m),
        };
        foreach (var p in pricing)
        {
            var entity = await _db.Products.FirstOrDefaultAsync(x => x.ProductCode == p.Code, ct);
            if (entity is not null && entity.ProviderCommissionRate is null)
            {
                entity.UpdateCommercialPricing(
                    wholesalePrice: null, consignmentPrice: p.Consignment, providerCommissionRate: p.Rate);
                _logger.LogInformation("Seeded commercial pricing on {Code}: consignment={Consignment}, rate={Rate}",
                    p.Code, p.Consignment, p.Rate);
            }
        }

        await _db.SaveChangesAsync(ct);
    }
}
