using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Domain.Entities;
using Aizen.Modules.CargoDry.Repository.Persistence;
using Aizen.Modules.CargoDry.Repository.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.CargoDry.Repository.UnitTests;

/// <summary>
/// CargoDry supply v2 ADDENDUM A1 — the accept stock gate counts only the provider's OWN AVAILABLE consignment kits of
/// the product (not other providers, not other products, not already-activated kits).
/// </summary>
public sealed class CargoDryAvailableStockGateTests
{
    private static CargoDryDbContext NewDb()
        => new(new DbContextOptionsBuilder<CargoDryDbContext>()
            .UseInMemoryDatabase($"cargodry-{Guid.NewGuid():N}").Options);

    private static CargoDryKitEntity AssignedAvailable(string serial, string productCode, long providerProfileId)
    {
        var kit = CargoDryKitEntity.Create(serial, $"K-{serial}", $"qr-{serial}", productCode, "B-1");
        kit.AssignToProvider(providerProfileId, SalesChannel.ConsignmentSellThrough, CargoDryCommercialModel.PrincipalSale);
        return kit; // AssignToProvider leaves Status == Available
    }

    [Fact]
    public async Task CountAvailable_counts_only_own_available_kits_of_the_product()
    {
        await using var db = NewDb();
        db.Kits.AddRange(
            AssignedAvailable("S1", "STANDARD-90", 100),   // ✓
            AssignedAvailable("S2", "STANDARD-90", 100),   // ✓
            AssignedAvailable("S3", "PREMIUM-180", 100),   // ✗ other product
            AssignedAvailable("S4", "STANDARD-90", 200));  // ✗ other provider
        var activated = AssignedAvailable("S5", "STANDARD-90", 100);
        activated.Activate(userId: 1, vesselId: 1, validityDays: 90);   // ✗ no longer Available
        db.Kits.Add(activated);
        await db.SaveChangesAsync();

        var repo = new CargoDryKitRepository(db);

        (await repo.CountAvailableForProviderProductAsync(100, "STANDARD-90", default)).Should().Be(2);
        (await repo.CountAvailableForProviderProductAsync(100, "PREMIUM-180", default)).Should().Be(1);
        (await repo.CountAvailableForProviderProductAsync(999, "STANDARD-90", default)).Should().Be(0, "no stock → accept must be blocked");
    }
}
