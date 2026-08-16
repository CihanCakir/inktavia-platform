using Aizen.Core.Data.Mongo;
using Aizen.Core.Data.Mongo.Repository;
using Aizen.Modules.ReferenceData.Domain.Documents.Location;
using Aizen.Modules.ReferenceData.Repository.Context;
using Aizen.Modules.ReferenceData.Repository.Mongo;
using Microsoft.Extensions.Logging;
using MongoDB.Driver.Linq;

namespace Aizen.Modules.ReferenceData.Repository.Seed.Services;

/// <summary>
/// M3-1a — assigns a deterministic, globally-unique, reserved-guarded <c>Slug</c> to every country/city/district/
/// neighborhood document that lacks one. Runs at startup after the location docs are seeded.
///
/// DETERMINISM: processes strictly by level (country → city → district → neighborhood) then by code, and a document
/// that already carries a slug is only RESERVED (never reassigned). So a re-run — or a resumed partial run — always
/// yields the same slugs, and a fully-slugged dataset is a NO-OP.
///
/// SCHEME: slug from <c>Name["tr"]</c> → <c>Name["en"]</c> → first available (Turkish market), via
/// <see cref="LocationSlugifier"/>. Lower levels are PARENT-QUALIFIED for meaningfulness and fewer collisions:
/// country=<c>{name}</c>, city=<c>{name}</c>, district=<c>{cityName}-{name}</c>,
/// neighborhood=<c>{districtName}-{name}</c>. Global uniqueness across ALL four levels is enforced with a numeric
/// suffix (<c>kadikoy-2</c>); reserved slugs (W3.4) are pre-seeded as used so any collision is suffixed.
/// </summary>
public sealed class LocationSlugBackfillService
{
    private readonly IAizenMongoRepository<LocationCountryDocument> _countries;
    private readonly IAizenMongoRepository<LocationCityDocument> _cities;
    private readonly IAizenMongoRepository<LocationDistrictDocument> _districts;
    private readonly IAizenMongoRepository<LocationNeighborhoodDocument> _neighborhoods;
    private readonly ILogger<LocationSlugBackfillService> _logger;

    public LocationSlugBackfillService(
        IAizenMongoRepositoryFactory<ReferenceDataMongoDbContext> factory,
        ILogger<LocationSlugBackfillService> logger)
    {
        _countries = factory.GetRepository<LocationCountryDocument>();
        _cities = factory.GetRepository<LocationCityDocument>();
        _districts = factory.GetRepository<LocationDistrictDocument>();
        _neighborhoods = factory.GetRepository<LocationNeighborhoodDocument>();
        _logger = logger;
    }

    public async Task BackfillAsync(CancellationToken cancellationToken = default)
    {
        // Global slug registry (pure), pre-seeded with reserved slugs so a base that lands on one is suffixed away.
        var registry = new LocationSlugRegistry();
        var assigned = 0;

        // Parent name-parts (own-name slug, pre-suffix), built as we go, keyed by full code path.
        var cityNamePart = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var districtNamePart = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // ── Countries ─────────────────────────────────────────────────────────────
        var countries = await _countries.FindManyAsync(
            predicate: null,
            orderBy: q => (IOrderedMongoQueryable<LocationCountryDocument>)q.OrderBy(x => x.CountryCode),
            topCount: -1, cancellationToken: cancellationToken);
        foreach (var c in countries)
            assigned += await AssignAsync(_countries, c, c.Slug, Base(Name(c.Name, c.CountryCode)), registry, cancellationToken);

        // ── Cities ────────────────────────────────────────────────────────────────
        var cities = await _cities.FindManyAsync(
            predicate: null,
            orderBy: q => (IOrderedMongoQueryable<LocationCityDocument>)q.OrderBy(x => x.CountryCode).ThenBy(x => x.CityCode),
            topCount: -1, cancellationToken: cancellationToken);
        foreach (var c in cities)
        {
            var namePart = Base(Name(c.Name, c.CityCode));
            cityNamePart[CityKey(c.CountryCode, c.CityCode)] = namePart;
            assigned += await AssignAsync(_cities, c, c.Slug, namePart, registry, cancellationToken);
        }

        // ── Districts (parent-qualified by city name) ──────────────────────────────
        var districts = await _districts.FindManyAsync(
            predicate: null,
            orderBy: q => (IOrderedMongoQueryable<LocationDistrictDocument>)q
                .OrderBy(x => x.CountryCode).ThenBy(x => x.CityCode).ThenBy(x => x.DistrictCode),
            topCount: -1, cancellationToken: cancellationToken);
        foreach (var d in districts)
        {
            var own = Base(Name(d.Name, d.DistrictCode));
            districtNamePart[DistrictKey(d.CountryCode, d.CityCode, d.DistrictCode)] = own;
            var parent = cityNamePart.GetValueOrDefault(CityKey(d.CountryCode, d.CityCode));
            assigned += await AssignAsync(_districts, d, d.Slug, Qualify(parent, own), registry, cancellationToken);
        }

        // ── Neighborhoods (parent-qualified by district name) ──────────────────────
        var neighborhoods = await _neighborhoods.FindManyAsync(
            predicate: null,
            orderBy: q => (IOrderedMongoQueryable<LocationNeighborhoodDocument>)q
                .OrderBy(x => x.CountryCode).ThenBy(x => x.CityCode).ThenBy(x => x.DistrictCode).ThenBy(x => x.NeighborhoodCode),
            topCount: -1, cancellationToken: cancellationToken);
        foreach (var n in neighborhoods)
        {
            var own = Base(Name(n.Name, n.NeighborhoodCode));
            var parent = districtNamePart.GetValueOrDefault(DistrictKey(n.CountryCode, n.CityCode, n.DistrictCode));
            assigned += await AssignAsync(_neighborhoods, n, n.Slug, Qualify(parent, own), registry, cancellationToken);
        }

        _logger.LogInformation("M3 location slug backfill: assigned {Assigned} new slugs.", assigned);
    }

    // Reserve an existing slug; or assign the first globally-unique candidate for a base and persist. Returns 1 if a
    // new slug was written, else 0 (idempotent — a doc that already has a slug is only registered, never rewritten).
    private static async Task<int> AssignAsync<TDoc>(
        IAizenMongoRepository<TDoc> repo, TDoc doc, string? existingSlug, string baseSlug,
        LocationSlugRegistry registry, CancellationToken ct)
        where TDoc : Aizen.Core.Data.Mongo.Document.AizenDocumentBase, Domain.Documents.Location.ISluggableLocation
    {
        if (!string.IsNullOrWhiteSpace(existingSlug))
        {
            registry.Reserve(existingSlug!);
            return 0;
        }

        doc.Slug = registry.Assign(baseSlug);
        await repo.ReplaceAsync(doc, ct);
        return 1;
    }

    private static string Name(IReadOnlyDictionary<string, string> name, string fallbackCode)
        => name.GetValueOrDefault("tr") ?? name.GetValueOrDefault("en") ?? name.Values.FirstOrDefault() ?? fallbackCode;

    private static string Base(string source)
    {
        var slug = LocationSlugifier.Slugify(source);
        return string.IsNullOrEmpty(slug) ? "location" : slug;
    }

    private static string Qualify(string? parentPart, string ownPart)
        => string.IsNullOrEmpty(parentPart) ? ownPart : $"{parentPart}-{ownPart}";

    private static string CityKey(string country, string city) => $"{country}|{city}";
    private static string DistrictKey(string country, string city, string district) => $"{country}|{city}|{district}";
}
