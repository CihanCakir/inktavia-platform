using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Aizen.Modules.ReferenceData.Repository.Context;
using Aizen.Modules.ReferenceData.Repository.Options;
using Aizen.Modules.ReferenceData.Repository.Repositories.Catalog;
using Aizen.Modules.ReferenceData.Repository.Seed.Readers;
using Aizen.Modules.ReferenceData.Repository.Seed.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace Aizen.Modules.ReferenceData.Repository.UnitTests.Catalog;

/// <summary>
/// Loads the shipped catalog seed JSON (the files the <c>tools/catalog-seed</c> generator emits) through the real
/// <see cref="CatalogJsonSeedService"/> into an in-memory ReferenceData context — proving the JSON deserializes into
/// the seed models, every model resolves its brand, and per-row <c>source</c>/<c>needsReview</c> survive to the entity.
/// </summary>
public sealed class CatalogSeedLoadTests
{
    // Source-tree path of the seed JSON, resolved at compile time so the test is machine-independent.
    private static string SeedJsonRoot([CallerFilePath] string thisFile = "")
    {
        // <module>/tests/<UnitTests>/Catalog/CatalogSeedLoadTests.cs → up 3 → <module> (Modules/ReferenceData)
        var moduleRoot = Directory.GetParent(thisFile)!.Parent!.Parent!.Parent!.FullName;
        return Path.Combine(moduleRoot, "src", "Aizen.Modules.ReferenceData.Repository", "Seed", "Json");
    }

    private static ReferenceDataDbContext NewDb()
        => new(new DbContextOptionsBuilder<ReferenceDataDbContext>()
            .UseInMemoryDatabase($"catalog-seed-{System.Guid.NewGuid():N}")
            .Options);

    private static CatalogJsonSeedService NewService(ReferenceDataDbContext db)
    {
        var reader = new ReferenceDataJsonSeedReader(Microsoft.Extensions.Options.Options.Create(new ReferenceDataSeedOptions
        {
            JsonRootPath = SeedJsonRoot(),
            FailOnMissingRequiredFile = false,
        }));
        return new CatalogJsonSeedService(
            new VesselBrandRepository(db), new VesselModelRepository(db),
            new EngineBrandRepository(db), new EngineModelRepository(db), db, reader);
    }

    [Fact]
    public async Task Seed_Loads_Full_Catalog_From_Shipped_Json()
    {
        var db = NewDb();

        await NewService(db).SeedAsync();

        // The generator ships the v1.1 catalog (dozens of brands, 1000+ vessel / 400+ engine models).
        db.VesselBrands.Count().Should().BeGreaterThan(50);
        db.VesselModels.Count().Should().BeGreaterThan(900);
        db.EngineBrands.Count().Should().BeGreaterThan(18);
        db.EngineModels.Count().Should().BeGreaterThan(380);

        // Every engine model carries HP (the selection point — required, non-null).
        db.EngineModels.All(m => m.HorsePower != null).Should().BeTrue();

        // Every model resolved a real brand (no orphans left behind by the code→id resolution).
        var vBrandIds = db.VesselBrands.Select(b => b.Id).ToHashSet();
        db.VesselModels.All(m => vBrandIds.Contains(m.VesselBrandId)).Should().BeTrue();
        var eBrandIds = db.EngineBrands.Select(b => b.Id).ToHashSet();
        db.EngineModels.All(m => eBrandIds.Contains(m.EngineBrandId)).Should().BeTrue();

        // Per-row provenance survived to the entity (not the "seed" fallback for a sourced row).
        var beneteau = db.VesselBrands.Single(b => b.Code == "BENETEAU");
        beneteau.Source.Should().Contain("beneteau.com");
        beneteau.NeedsReview.Should().BeFalse();

        // Undocumented production start → nullable YearFrom + NeedsReview flag (v1 gap contract).
        db.VesselModels.Any(m => m.YearFrom == null && m.NeedsReview).Should().BeTrue();

        // Engine HP carried through for a documented outboard family.
        var f150 = db.EngineModels.Single(m => m.Code == "F150");
        f150.HorsePower.Should().Be(150);

        // VesselTypeCode is the REAL MARINE VESSEL_TYPE lookup vocabulary (so the picker's ?typeCode= equality filter
        // matches) — not the coarse guess codes. No legacy label survives the generator mapping.
        var realTypeCodes = new[] { "MOTOR_YACHT", "SAILING_BOAT", "CATAMARAN", "RIB", "JET_SKI" };
        db.VesselModels.Select(m => m.VesselTypeCode).Distinct().ToList()
            .Should().OnlyContain(c => c == null || realTypeCodes.Contains(c));
        db.VesselModels.Any(m => m.VesselTypeCode == "MOTOR_YACHT").Should().BeTrue();
        db.VesselModels.Any(m => m.VesselTypeCode == "MOTORYACHT" || m.VesselTypeCode == "SAILBOAT"
            || m.VesselTypeCode == "PWC" || m.VesselTypeCode == "SUPERYACHT").Should().BeFalse();
    }

    [Fact]
    public async Task Seed_Is_Idempotent_On_Rerun()
    {
        var db = NewDb();
        var svc = NewService(db);

        await svc.SeedAsync();
        var brands = db.VesselBrands.Count();
        var models = db.VesselModels.Count();

        await svc.SeedAsync(); // second pass upserts by code — no duplicates

        db.VesselBrands.Count().Should().Be(brands);
        db.VesselModels.Count().Should().Be(models);
    }
}
