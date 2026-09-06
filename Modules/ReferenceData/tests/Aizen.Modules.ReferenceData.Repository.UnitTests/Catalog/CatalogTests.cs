using Aizen.Modules.ReferenceData.Abstraction.Request.Catalog;
using Aizen.Modules.ReferenceData.Domain.Entities.Catalog;
using Aizen.Modules.ReferenceData.Repository.Context;
using Aizen.Modules.ReferenceData.Repository.Repositories.Catalog;
using Aizen.Modules.ReferenceData.Repository.Service;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Aizen.Modules.ReferenceData.Repository.UnitTests.Catalog;

public sealed class CatalogNameNormalizerTests
{
    [Theory]
    [InlineData("Beneteau", "Beneteau")]
    [InlineData("  Beneteau ", "Beneteau")]
    [InlineData("Sea   Ray", "Sea Ray")]
    public void Clean_Trims_And_Collapses_Whitespace(string input, string expected)
        => CatalogNameNormalizer.Clean(input).Should().Be(expected);

    [Theory]
    [InlineData("Beneteau", "BENETEAU")]
    [InlineData("  beneteau ", "BENETEAU")]
    [InlineData("BENETEAU", "BENETEAU")]
    [InlineData("Sea  Ray", "SEA RAY")]
    public void Key_Is_Case_And_Whitespace_Insensitive(string input, string expectedKey)
        => CatalogNameNormalizer.Key(input).Should().Be(expectedKey);

    [Fact]
    public void Variants_Share_One_Dedupe_Key()
    {
        var a = CatalogNameNormalizer.Key("Beneteau");
        CatalogNameNormalizer.Key("  BENETEAU ").Should().Be(a);
        CatalogNameNormalizer.Key("beneteau").Should().Be(a);
    }

    [Theory]
    [InlineData("Sea Ray", "SEA_RAY")]
    [InlineData("Volvo Penta!!", "VOLVO_PENTA")]
    [InlineData("  ", "ITEM")]
    public void Slug_Is_An_Opaque_Uppercase_Code(string input, string expected)
        => CatalogNameNormalizer.Slug(input).Should().Be(expected);
}

public sealed class CatalogSubmissionDedupeTests
{
    private static ReferenceDataDbContext NewDb()
        => new(new DbContextOptionsBuilder<ReferenceDataDbContext>()
            .UseInMemoryDatabase($"catalog-{System.Guid.NewGuid():N}")
            .Options);

    private static VesselCatalogReferenceService NewVesselSvc(ReferenceDataDbContext db)
        => new(new VesselBrandRepository(db), new VesselModelRepository(db), db);

    [Fact]
    public async Task Submit_Brand_Creates_NeedsReview_But_Active()
    {
        var db = NewDb();
        var svc = NewVesselSvc(db);

        var b = await svc.SubmitBrandAsync(new SubmitVesselBrandRequest { Name = "Beneteau", CountryCode = "fr" });

        b.NeedsReview.Should().BeTrue();
        b.IsActive.Should().BeTrue();
        b.Name.Should().Be("Beneteau");
        b.CountryCode.Should().Be("FR");
        db.VesselBrands.Count().Should().Be(1);
    }

    [Fact]
    public async Task Submit_Brand_Dedupes_By_Normalized_Name()
    {
        var db = NewDb();
        var svc = NewVesselSvc(db);

        var first = await svc.SubmitBrandAsync(new SubmitVesselBrandRequest { Name = "Beneteau" });
        var dup   = await svc.SubmitBrandAsync(new SubmitVesselBrandRequest { Name = "  BENETEAU " });
        var other = await svc.SubmitBrandAsync(new SubmitVesselBrandRequest { Name = "Jeanneau" });

        dup.Id.Should().Be(first.Id);           // same normalized name → existing row returned
        other.Id.Should().NotBe(first.Id);
        db.VesselBrands.Count().Should().Be(2); // only two distinct brands persisted
    }

    [Fact]
    public async Task Submit_Model_Dedupes_Within_A_Brand()
    {
        var db = NewDb();
        var svc = NewVesselSvc(db);
        var brand = await svc.SubmitBrandAsync(new SubmitVesselBrandRequest { Name = "Beneteau" });

        var m1  = await svc.SubmitModelAsync(new SubmitVesselModelRequest { VesselBrandId = brand.Id, Name = "Oceanis 40.1", YearFrom = 2019 });
        var dup = await svc.SubmitModelAsync(new SubmitVesselModelRequest { VesselBrandId = brand.Id, Name = "  oceanis 40.1 ", YearFrom = 2019 });

        dup.Id.Should().Be(m1.Id);
        dup.BrandName.Should().Be("Beneteau");  // denormalized brand name surfaced
        db.VesselModels.Count().Should().Be(1);
    }
}
