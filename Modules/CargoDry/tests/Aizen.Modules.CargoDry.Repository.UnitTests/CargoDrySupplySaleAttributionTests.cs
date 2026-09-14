using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Domain.Entities;
using Aizen.Modules.CargoDry.Repository.Persistence;
using Aizen.Modules.CargoDry.Repository.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.CargoDry.Repository.UnitTests;

/// <summary>
/// CargoDry supply flow guards:
///   (b) refund-before-activation → NO attribution is recorded for the SR (the sale is recorded ONLY on activation).
///   (c) attribution idempotency keyed by source SR id — a re-fired activation can never double-credit the provider.
/// </summary>
public sealed class CargoDrySupplySaleAttributionTests
{
    private static readonly DateTime NowUtc = new(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);

    private static CargoDryDbContext NewDb()
        => new(new DbContextOptionsBuilder<CargoDryDbContext>()
            .UseInMemoryDatabase($"cargodry-{Guid.NewGuid():N}").Options);

    private static CargoDrySalesAttributionEntity NewConsignmentAttribution()
        => CargoDrySalesAttributionEntity.Create(
            kitId:            10,
            serialNumber:     "SN-10",
            kitCode:          "KIT-10",
            productCode:      "STANDARD-90",
            batchCode:        "B-1",
            salesChannel:     SalesChannel.ConsignmentSellThrough,
            commercialModel:  CargoDryCommercialModel.PrincipalSale,
            initialStatus:    CargoDrySalesAttributionStatus.SettlementPending,
            nowUtc:           NowUtc,
            providerProfileId: 100011);

    // ── (c) set-once link ──────────────────────────────────────────────────────
    [Fact]
    public void LinkSupplyServiceRequest_is_set_once_and_throws_on_second_call()
    {
        var attribution = NewConsignmentAttribution();

        attribution.LinkSupplyServiceRequest(500);
        attribution.SourceServiceRequestId.Should().Be(500);

        var act = () => attribution.LinkSupplyServiceRequest(999);
        act.Should().Throw<InvalidOperationException>("a second activation event must not re-link / double-credit");
        attribution.SourceServiceRequestId.Should().Be(500, "the original link is preserved");
    }

    // ── (b)+(c) the idempotency lookup ─────────────────────────────────────────
    [Fact]
    public async Task GetBySourceServiceRequestIdAsync_is_null_before_link_and_returns_the_row_after()
    {
        await using var db = NewDb();
        var repo = new CargoDrySalesAttributionRepository(db);

        // Activation created the attribution, but NO supply sale has been recorded yet (SourceServiceRequestId null).
        // This is exactly the refund-before-activation state: querying by the SR id finds nothing → no provider credit.
        var attribution = NewConsignmentAttribution();
        await repo.AddAsync(attribution, default);
        await repo.SaveChangesAsync(default);

        (await repo.GetBySourceServiceRequestIdAsync(500, default))
            .Should().BeNull("no attribution is recorded for the SR until the kit is activated");

        // Record the supply sale (link the SR). Now the idempotency lookup finds exactly this row.
        attribution.LinkSupplyServiceRequest(500);
        await repo.SaveChangesAsync(default);

        var found = await repo.GetBySourceServiceRequestIdAsync(500, default);
        found.Should().NotBeNull();
        found!.Id.Should().Be(attribution.Id, "the idempotency guard resolves the one attribution for the SR");
    }
}
