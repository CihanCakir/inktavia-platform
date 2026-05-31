using Aizen.Modules.ReferenceData.Domain.Interface;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;
using Aizen.Modules.ReferenceData.Repository.Context;
using Aizen.Modules.ReferenceData.Repository.Mongo;
using Aizen.Modules.ReferenceData.Repository.Repositories.Currency;
using Aizen.Modules.ReferenceData.Repository.Repositories.ExchangeRate;
using Aizen.Modules.ReferenceData.Repository.Repositories.LookupGroup;
using Aizen.Modules.ReferenceData.Repository.Repositories.Measurement;
using Aizen.Modules.ReferenceData.Repository.Repositories.SystemParameter;
using Aizen.Modules.ReferenceData.Repository.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Aizen.Modules.ReferenceData.Repository;

public static class DependencyInjection
{
    public static IServiceCollection AddReferenceDataRepository(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Identity ve InktaviaStore.Repository standardına göre gerekirse değiştir.
        services.AddDbContext<ReferenceDataDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("ReferenceDataPostgreSql");
            options.UseNpgsql(connectionString);
        });

        // Aizen Mongo registration standardına göre gerekirse options pattern'e çek.
        services.AddSingleton<IMongoClient>(_ =>
        {
            var connectionString = configuration.GetConnectionString("ReferenceDataMongo");
            return new MongoClient(connectionString);
        });

        services.AddScoped(sp =>
        {
            var databaseName = configuration.GetValue<string>("ReferenceDataMongo:DatabaseName") ?? "reference_data";
            var client = sp.GetRequiredService<IMongoClient>();
            return client.GetDatabase(databaseName);
        });

        services.AddScoped<ICurrencyRepository, CurrencyRepository>();
        services.AddScoped<IExchangeRateRepository, ExchangeRateRepository>();
        services.AddScoped<ILookupGroupRepository, LookupGroupRepository>();
        services.AddScoped<ILookupItemRepository, LookupItemRepository>();
        services.AddScoped<IMeasurementUnitRepository, MeasurementUnitRepository>();
        services.AddScoped<ISystemParameterRepository, SystemParameterRepository>();
        services.AddScoped<ILocationRepository, LocationRepository>();
        services.AddScoped<ReferenceDataMongoIndexInitializer>();

        return services;
    }

    public static IServiceCollection AddReferenceDataServices(this IServiceCollection services)
    {
        services.AddScoped<ICurrencyReferenceService, CurrencyReferenceService>();
        services.AddScoped<IExchangeRateReferenceService, ExchangeRateReferenceService>();
        services.AddScoped<ILookupReferenceService, LookupReferenceService>();
        services.AddScoped<ILookupTreeService, LookupTreeService>();
        services.AddScoped<IMeasurementReferenceService, MeasurementReferenceService>();
        services.AddScoped<ILocationReferenceService, LocationReferenceService>();
        services.AddScoped<ILocationValidationService, LocationValidationService>();
        services.AddScoped<ISystemParameterReferenceService, SystemParameterReferenceService>();
        services.AddScoped<IReferenceDataCacheKeyService, ReferenceDataCacheKeyService>();
        services.AddScoped<IReferenceDataCacheInvalidationService, ReferenceDataCacheInvalidationService>();
        services.AddScoped<IReferenceDataMongoIndexService, ReferenceDataMongoIndexService>();
        services.AddScoped<IReferenceDataSeedService, ReferenceDataSeedService>();

        return services;
    }
}
