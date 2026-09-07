using System.Linq;
using System.Threading.Tasks;
using Aizen.Modules.Vessel.Abstraction.Dto.CatalogReference;
using Aizen.Modules.Vessel.Domain.Entities.Vessel;
using Aizen.Modules.Vessel.Repository.Persistence;
using Aizen.Modules.Vessel.Repository.Repositories.CatalogReference;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Vessel.Repository.UnitTests;

/// <summary>
/// The catalog-merge repoint moves every vessel reference from a source catalog id to the target, in one
/// transaction, returning the moved count — and is idempotent (a re-run moves 0). Backs item-5 "repoint count correct".
/// </summary>
public sealed class CatalogReferenceRepointTests
{
    private static VesselDbContext NewDb()
        => new(new DbContextOptionsBuilder<VesselDbContext>()
            .UseInMemoryDatabase($"vessel-repoint-{System.Guid.NewGuid():N}").Options);

    private static VesselSpecificationEntity Spec(long vesselId, long? brandId, long? modelId)
    {
        var s = VesselSpecificationEntity.Create(vesselId, "b", "m", null, null, null, null, null, null, null,
            null, null, null, null, null, null, null, null, null, null);
        s.SetBrandModel(brandId, modelId);
        return s;
    }

    private static VesselEngineEntity Engine(long vesselId, long? brandId, long? modelId)
    {
        var e = VesselEngineEntity.Create(vesselId, "eng", "INBOARD", "DIESEL", "b", "m", null, 100, null, true);
        e.SetBrandModel(brandId, modelId);
        return e;
    }

    [Fact]
    public async Task Repoint_VesselBrand_Moves_All_Refs_And_Counts()
    {
        var db = NewDb();
        db.VesselSpecifications.AddRange(Spec(1, 100, 10), Spec(2, 100, 11), Spec(3, 200, 12));
        await db.SaveChangesAsync();
        var repo = new VesselCatalogReferenceRepository(db);

        var moved = await repo.RepointAsync(CatalogRefKind.VesselBrand, sourceId: 100, targetId: 200);

        moved.Should().Be(2);
        db.VesselSpecifications.Count(s => s.VesselBrandId == 100).Should().Be(0);
        db.VesselSpecifications.Count(s => s.VesselBrandId == 200).Should().Be(3);
        // the model ids on the moved rows are preserved (only the brand fk repointed)
        db.VesselSpecifications.Single(s => s.VesselId == 1).VesselModelId.Should().Be(10);
    }

    [Fact]
    public async Task Repoint_Is_Idempotent()
    {
        var db = NewDb();
        db.VesselSpecifications.AddRange(Spec(1, 100, 10), Spec(2, 100, 11));
        await db.SaveChangesAsync();
        var repo = new VesselCatalogReferenceRepository(db);

        (await repo.RepointAsync(CatalogRefKind.VesselBrand, 100, 200)).Should().Be(2);
        (await repo.RepointAsync(CatalogRefKind.VesselBrand, 100, 200)).Should().Be(0); // nothing left to move
    }

    [Fact]
    public async Task Repoint_EngineModel_Moves_Only_Matching_And_Preserves_Brand()
    {
        var db = NewDb();
        db.VesselEngines.AddRange(Engine(1, 5, 50), Engine(2, 5, 50), Engine(3, 5, 99));
        await db.SaveChangesAsync();
        var repo = new VesselCatalogReferenceRepository(db);

        var moved = await repo.RepointAsync(CatalogRefKind.EngineModel, sourceId: 50, targetId: 51);

        moved.Should().Be(2);
        db.VesselEngines.Count(e => e.EngineModelId == 51).Should().Be(2);
        db.VesselEngines.Where(e => e.EngineModelId == 51).All(e => e.EngineBrandId == 5).Should().BeTrue();
        db.VesselEngines.Count(e => e.EngineModelId == 99).Should().Be(1);
    }

    [Fact]
    public async Task Counts_Group_By_Catalog_Id()
    {
        var db = NewDb();
        db.VesselSpecifications.AddRange(Spec(1, 100, 10), Spec(2, 100, 11), Spec(3, 200, 10));
        db.VesselEngines.AddRange(Engine(1, 5, 50), Engine(2, 5, 51));
        await db.SaveChangesAsync();
        var repo = new VesselCatalogReferenceRepository(db);

        var counts = await repo.GetReferenceCountsAsync();

        counts.VesselBrand[100].Should().Be(2);
        counts.VesselBrand[200].Should().Be(1);
        counts.VesselModel[10].Should().Be(2);
        counts.EngineBrand[5].Should().Be(2);
    }
}
