using Aizen.Modules.CargoDry.Domain.Entities;
using Aizen.Modules.CargoDry.Repository.Persistence;
using Aizen.Modules.CargoDry.Repository.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.CargoDry.Repository.UnitTests;

/// <summary>
/// CargoDry supply v2 — cargo (direct online) sale revenue record is idempotent per source SR: the lookup finds
/// nothing before the sale is recorded and exactly the one row after, so a re-fired completion cannot double-count.
/// </summary>
public sealed class CargoDryDirectSaleTests
{
    private static CargoDryDbContext NewDb()
        => new(new DbContextOptionsBuilder<CargoDryDbContext>()
            .UseInMemoryDatabase($"cargodry-{Guid.NewGuid():N}").Options);

    [Fact]
    public async Task GetBySourceServiceRequestId_is_null_before_and_returns_the_row_after()
    {
        await using var db = NewDb();
        var repo = new CargoDryDirectSaleRepository(db);

        (await repo.GetBySourceServiceRequestIdAsync(777, default)).Should().BeNull();

        var sale = CargoDryDirectSaleEntity.Create(
            sourceServiceRequestId: 777, productCode: "STANDARD-90", saleAmount: 149.99m, currencyCode: "TRY",
            completedAtUtc: new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc), trackingCode: "TRACK1");
        await repo.AddAsync(sale, default);
        await repo.SaveChangesAsync(default);

        var found = await repo.GetBySourceServiceRequestIdAsync(777, default);
        found.Should().NotBeNull();
        found!.SaleAmount.Should().Be(149.99m);
        found.ProductCode.Should().Be("STANDARD-90");
    }
}
