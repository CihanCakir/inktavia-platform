using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aizen.Modules.ReferenceData.Repository.Context;
using Aizen.Modules.ReferenceData.Repository.Repositories.Marina;
using Aizen.Modules.ReferenceData.Repository.Seed.Models.Marina;
using Aizen.Modules.ReferenceData.Repository.Seed.Readers;
using Aizen.Modules.ReferenceData.Repository.Seed.Services;
using Aizen.Modules.ReferenceData.Repository.Service;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Aizen.Modules.ReferenceData.Repository.UnitTests.Marina;

/// <summary>
/// The marina JSON seeder must NOT clobber admin curation on re-run: once a row is admin-edited, re-seeding skips the
/// field-overwrite for it — but still adds brand-new rows. Regression for the "curation wiped on every deploy" risk.
/// </summary>
public sealed class MarinaSeedGuardTests
{
    // In-test reader that returns a swappable marina list (other reads unused by the marina seeder).
    private sealed class FakeReader : IReferenceDataJsonSeedReader
    {
        public List<MarinaSeedModel> Marinas { get; set; } = new();
        public Task<IReadOnlyList<T>> ReadListAsync<T>(string relativePath, bool optional = false, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<T>>((IReadOnlyList<T>)(object)Marinas.Cast<object>().Cast<T>().ToList());
        public Task<T?> ReadSingleAsync<T>(string relativePath, bool optional = false, CancellationToken ct = default)
            => Task.FromResult<T?>(default);
        public Task<IReadOnlyList<T>> ReadAllInDirectoryAsync<T>(string relativeDirectory, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<T>>(new List<T>());
    }

    private static ReferenceDataDbContext NewDb()
        => new(new DbContextOptionsBuilder<ReferenceDataDbContext>()
            .UseInMemoryDatabase($"marina-seed-{System.Guid.NewGuid():N}").Options);

    private static MarinaSeedModel Model(string code, string name, bool needsReview = true, bool active = true) => new()
    {
        Code = code, Name = name, Type = "MARINA", CountryCode = "TR", CityCode = "34",
        Province = "İstanbul", Latitude = 40.97m, Longitude = 29.05m, OsmId = "n1", NeedsReview = needsReview, IsActive = active,
    };

    [Fact]
    public async Task AdminEdit_Survives_Reseed_While_New_Rows_Still_Added()
    {
        var db = NewDb();
        var reader = new FakeReader { Marinas = new() { Model("M1", "Marina (İstanbul)") } };
        var seeder = new MarinaJsonSeedService(new MarinaRepository(db), db, reader);

        // 1) first seed
        await seeder.SeedAsync();
        var m1 = db.Marinas.Single(x => x.Code == "M1");
        m1.Name.Should().Be("Marina (İstanbul)");
        m1.IsAdminEdited.Should().BeFalse();

        // 2) admin curates M1 (real write path) — renames + clears review
        var svc = new MarinaReferenceService(new MarinaRepository(db), db);
        (await svc.UpdateAsync(m1.Id, "Setur Kalamış Marina", "34")).Should().BeTrue();
        (await svc.MarkReviewedAsync(m1.Id)).Should().BeTrue();

        // 3) re-seed with a CHANGED M1 payload + a brand-new M2
        reader.Marinas = new()
        {
            Model("M1", "Marina (İstanbul)"),           // JSON still has the generic name + needsReview=true
            Model("M2", "Marina (İzmir)"),               // new row
        };
        await seeder.SeedAsync();

        // 4) M1 curation SURVIVED (name + cleared review kept), M2 was added
        var m1After = db.Marinas.Single(x => x.Code == "M1");
        m1After.Name.Should().Be("Setur Kalamış Marina");
        m1After.NeedsReview.Should().BeFalse();
        m1After.IsAdminEdited.Should().BeTrue();
        db.Marinas.Any(x => x.Code == "M2").Should().BeTrue();
    }

    [Fact]
    public async Task Non_Edited_Rows_Still_Refresh_From_Json()
    {
        var db = NewDb();
        var reader = new FakeReader { Marinas = new() { Model("M1", "Marina (İstanbul)") } };
        var seeder = new MarinaJsonSeedService(new MarinaRepository(db), db, reader);
        await seeder.SeedAsync();

        // No admin edit → re-seed with a corrected name should refresh (normal seed behaviour preserved).
        reader.Marinas = new() { Model("M1", "Ataköy Marina", needsReview: false) };
        await seeder.SeedAsync();

        var m1 = db.Marinas.Single(x => x.Code == "M1");
        m1.Name.Should().Be("Ataköy Marina");
        m1.NeedsReview.Should().BeFalse();
    }
}
