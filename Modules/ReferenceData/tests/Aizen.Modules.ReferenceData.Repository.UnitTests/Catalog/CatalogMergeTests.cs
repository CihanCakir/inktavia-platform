using System.Threading.Tasks;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Catalog;
using Aizen.Modules.ReferenceData.Domain.Entities.Catalog;
using Aizen.Modules.ReferenceData.Repository.Context;
using Aizen.Modules.ReferenceData.Repository.Repositories.Catalog;
using Aizen.Modules.ReferenceData.Repository.Service;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Aizen.Modules.ReferenceData.Repository.UnitTests.Catalog;

/// <summary>
/// Catalog-side merge: deactivate the source with a MergedIntoId audit, reject invalid merges (missing target =
/// cross-type; models from different brands), and stay idempotent on re-merge. The Vessel FK repoint is separate.
/// </summary>
public sealed class CatalogMergeTests
{
    private static ReferenceDataDbContext NewDb()
        => new(new DbContextOptionsBuilder<ReferenceDataDbContext>()
            .UseInMemoryDatabase($"catalog-merge-{System.Guid.NewGuid():N}").Options);

    private static CatalogMergeService Svc(ReferenceDataDbContext db) => new(
        new VesselBrandRepository(db), new VesselModelRepository(db),
        new EngineBrandRepository(db), new EngineModelRepository(db), db);

    private static VesselBrandEntity Brand(string code, string name) =>
        VesselBrandEntity.Create(code, name, "TR", needsReview: false, source: "seed");

    [Fact]
    public async Task Merge_VesselBrand_Deactivates_Source_With_Audit()
    {
        var db = NewDb();
        var source = Brand("DUP", "Beneteu");   // duplicate/typo
        var target = Brand("BENETEAU", "Beneteau");
        db.VesselBrands.AddRange(source, target);
        await db.SaveChangesAsync();

        var res = await Svc(db).MergeAsync(CatalogMergeType.VesselBrand, source.Id, target.Id);

        res.Success.Should().BeTrue();
        res.AlreadyMerged.Should().BeFalse();
        var reloaded = db.VesselBrands.Single(b => b.Id == source.Id);
        reloaded.IsActive.Should().BeFalse();
        reloaded.MergedIntoId.Should().Be(target.Id);
    }

    [Fact]
    public async Task Merge_Rejects_Same_Source_And_Target()
    {
        var db = NewDb();
        var b = Brand("X", "X"); db.VesselBrands.Add(b); await db.SaveChangesAsync();
        var act = () => Svc(db).MergeAsync(CatalogMergeType.VesselBrand, b.Id, b.Id);
        await act.Should().ThrowAsync<AizenBusinessException>().WithMessage("*different*");
    }

    [Fact]
    public async Task Merge_Rejects_When_Target_Not_Found_As_That_Type()
    {
        // Cross-type in effect: target id is not a vessel brand → not found → rejected (no silent cross-type merge).
        var db = NewDb();
        var source = Brand("DUP", "Dup"); db.VesselBrands.Add(source); await db.SaveChangesAsync();
        var act = () => Svc(db).MergeAsync(CatalogMergeType.VesselBrand, source.Id, targetId: 99999);
        await act.Should().ThrowAsync<AizenBusinessException>().WithMessage("*not found*");
    }

    [Fact]
    public async Task Merge_Models_Rejected_Across_Different_Brands()
    {
        var db = NewDb();
        var b1 = Brand("B1", "B1"); var b2 = Brand("B2", "B2");
        db.VesselBrands.AddRange(b1, b2); await db.SaveChangesAsync();
        var m1 = VesselModelEntity.Create(b1.Id, "M1", "Model 1", null, null, null, null, needsReview: false, source: "seed");
        var m2 = VesselModelEntity.Create(b2.Id, "M2", "Model 2", null, null, null, null, needsReview: false, source: "seed");
        db.VesselModels.AddRange(m1, m2); await db.SaveChangesAsync();

        var act = () => Svc(db).MergeAsync(CatalogMergeType.VesselModel, m1.Id, m2.Id);
        await act.Should().ThrowAsync<AizenBusinessException>().WithMessage("*same brand*");
    }

    [Fact]
    public async Task Re_Merge_Is_Idempotent_NoOp()
    {
        var db = NewDb();
        var source = Brand("DUP", "Dup"); var target = Brand("KEEP", "Keep");
        db.VesselBrands.AddRange(source, target); await db.SaveChangesAsync();
        var svc = Svc(db);

        (await svc.MergeAsync(CatalogMergeType.VesselBrand, source.Id, target.Id)).Success.Should().BeTrue();
        var second = await svc.MergeAsync(CatalogMergeType.VesselBrand, source.Id, target.Id);

        second.Success.Should().BeTrue();
        second.AlreadyMerged.Should().BeTrue();  // safe re-run
        db.VesselBrands.Single(b => b.Id == source.Id).MergedIntoId.Should().Be(target.Id);
    }
}
