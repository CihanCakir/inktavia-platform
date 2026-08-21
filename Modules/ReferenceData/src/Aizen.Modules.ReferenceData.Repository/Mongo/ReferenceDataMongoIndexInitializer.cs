using Aizen.Modules.ReferenceData.Domain.Documents.Location;
using Aizen.Modules.ReferenceData.Repository.Context;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Aizen.Modules.ReferenceData.Repository.Mongo;

public sealed class ReferenceDataMongoIndexInitializer
{
    private readonly IMongoDatabase _mongoDatabase;

    public ReferenceDataMongoIndexInitializer(ReferenceDataMongoDbContext mongoDbContext)
    {
        _mongoDatabase = mongoDbContext.Database;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await CreateCountryIndexesAsync(cancellationToken);
        await CreateCityIndexesAsync(cancellationToken);
        await CreateDistrictIndexesAsync(cancellationToken);
        await CreateNeighborhoodIndexesAsync(cancellationToken);
        await CreateStreetIndexesAsync(cancellationToken);
        await CreateSlugIndexesAsync(cancellationToken);
    }

    // M3 — koleksiyon başına BENZERSİZ slug index'i, { Slug: { $type: "string" } } üzerinde PARTIAL: yalnızca
    // GERÇEK (string) slug'lar benzersizlik kısıtına girer. Backfill öncesi slug'lar null'dır ve kısıt DIŞINDA
    // kalır. (Eski hâli $exists:true idi; Slug alanı null olarak da yazıldığından hiçbir şeyi dışlamıyordu ve
    // ikinci null slug { Slug: null } üzerinde E11000 veriyordu.) Küresel çapraz-seviye benzersizliği
    // deterministik backfill garanti eder; bu index'ler koleksiyon-içi benzersizliği korur ve by-slug aramalarına destek olur.
    private async Task CreateSlugIndexesAsync(CancellationToken cancellationToken)
    {
        await CreateSlugIndexAsync<LocationCountryDocument>(ReferenceDataMongoCollectionNames.Countries, "ux_country_slug", cancellationToken);
        await CreateSlugIndexAsync<LocationCityDocument>(ReferenceDataMongoCollectionNames.Cities, "ux_city_slug", cancellationToken);
        await CreateSlugIndexAsync<LocationDistrictDocument>(ReferenceDataMongoCollectionNames.Districts, "ux_district_slug", cancellationToken);
        await CreateSlugIndexAsync<LocationNeighborhoodDocument>(ReferenceDataMongoCollectionNames.Neighborhoods, "ux_neighborhood_slug", cancellationToken);
    }

    private async Task CreateSlugIndexAsync<TDoc>(string collectionName, string indexName, CancellationToken cancellationToken)
    {
        var collection = _mongoDatabase.GetCollection<TDoc>(collectionName);
        var options = new CreateIndexOptions<TDoc>
        {
            Unique = true,
            Name = indexName,
            // Yalnızca string slug'lar benzersizlik kısıtına girsin; null (veya alan yok) DIŞARIDA kalsın.
            PartialFilterExpression = Builders<TDoc>.Filter.Type("Slug", BsonType.String),
        };
        var model = new CreateIndexModel<TDoc>(Builders<TDoc>.IndexKeys.Ascending("Slug"), options);

        try
        {
            await collection.Indexes.CreateOneAsync(model, cancellationToken: cancellationToken);
        }
        catch (MongoCommandException ex) when (ex.Code is 85 or 86)
        {
            // 85 IndexOptionsConflict / 86 IndexKeySpecsConflict: aynı adlı index ESKİ ($exists) şekliyle zaten var.
            // MongoDB, aynı ad + farklı options ile createIndex'i reddeder. Kendi adımızla oluşturduğumuz index'i
            // (YALNIZCA onu, ada göre) düşürüp yeniden yaratırız — başkasının index'ine dokunmayız, çakışmayı da
            // sessizce yutmayız (yalnızca bilinen çakışma kodlarında düzeltiriz, diğer hatalar yukarı fırlar).
            await collection.Indexes.DropOneAsync(indexName, cancellationToken);
            await collection.Indexes.CreateOneAsync(model, cancellationToken: cancellationToken);
        }
    }

    private Task CreateCountryIndexesAsync(CancellationToken cancellationToken)
    {
        var collection = _mongoDatabase.GetCollection<LocationCountryDocument>(ReferenceDataMongoCollectionNames.Countries);
        var model = new CreateIndexModel<LocationCountryDocument>(
            Builders<LocationCountryDocument>.IndexKeys.Ascending(x => x.CountryCode),
            new CreateIndexOptions { Unique = true, Name = "ux_country_code" });

        return collection.Indexes.CreateOneAsync(model, cancellationToken: cancellationToken);
    }

    private Task CreateCityIndexesAsync(CancellationToken cancellationToken)
    {
        var collection = _mongoDatabase.GetCollection<LocationCityDocument>(ReferenceDataMongoCollectionNames.Cities);
        var model = new CreateIndexModel<LocationCityDocument>(
            Builders<LocationCityDocument>.IndexKeys.Ascending(x => x.CountryCode).Ascending(x => x.CityCode),
            new CreateIndexOptions { Unique = true, Name = "ux_country_city" });

        return collection.Indexes.CreateOneAsync(model, cancellationToken: cancellationToken);
    }

    private Task CreateDistrictIndexesAsync(CancellationToken cancellationToken)
    {
        var collection = _mongoDatabase.GetCollection<LocationDistrictDocument>(ReferenceDataMongoCollectionNames.Districts);
        var model = new CreateIndexModel<LocationDistrictDocument>(
            Builders<LocationDistrictDocument>.IndexKeys.Ascending(x => x.CountryCode).Ascending(x => x.CityCode).Ascending(x => x.DistrictCode),
            new CreateIndexOptions { Unique = true, Name = "ux_country_city_district" });

        return collection.Indexes.CreateOneAsync(model, cancellationToken: cancellationToken);
    }

    private Task CreateNeighborhoodIndexesAsync(CancellationToken cancellationToken)
    {
        var collection = _mongoDatabase.GetCollection<LocationNeighborhoodDocument>(ReferenceDataMongoCollectionNames.Neighborhoods);
        var model = new CreateIndexModel<LocationNeighborhoodDocument>(
            Builders<LocationNeighborhoodDocument>.IndexKeys.Ascending(x => x.CountryCode).Ascending(x => x.CityCode).Ascending(x => x.DistrictCode).Ascending(x => x.NeighborhoodCode),
            new CreateIndexOptions { Unique = true, Name = "ux_country_city_district_neighborhood" });

        return collection.Indexes.CreateOneAsync(model, cancellationToken: cancellationToken);
    }

    private Task CreateStreetIndexesAsync(CancellationToken cancellationToken)
    {
        var collection = _mongoDatabase.GetCollection<LocationStreetDocument>(ReferenceDataMongoCollectionNames.Streets);
        var model = new CreateIndexModel<LocationStreetDocument>(
            Builders<LocationStreetDocument>.IndexKeys.Ascending(x => x.CountryCode).Ascending(x => x.CityCode).Ascending(x => x.DistrictCode).Ascending(x => x.NeighborhoodCode).Ascending(x => x.StreetCode),
            new CreateIndexOptions { Name = "ix_country_city_district_neighborhood_street" });

        return collection.Indexes.CreateOneAsync(model, cancellationToken: cancellationToken);
    }
}
