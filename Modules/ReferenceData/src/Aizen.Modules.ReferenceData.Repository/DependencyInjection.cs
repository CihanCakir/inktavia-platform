using Aizen.Modules.ReferenceData.Domain.Interface;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;
using Aizen.Modules.ReferenceData.Repository.Context;
using Aizen.Modules.ReferenceData.Repository.Mongo;
using Aizen.Modules.ReferenceData.Repository.Options;
using Aizen.Modules.ReferenceData.Repository.Repositories.Currency;
using Aizen.Modules.ReferenceData.Repository.Repositories.ExchangeRate;
using Aizen.Modules.ReferenceData.Repository.Repositories.LookupGroup;
using Aizen.Modules.ReferenceData.Repository.Repositories.Measurement;
using Aizen.Modules.ReferenceData.Repository.Repositories.SystemParameter;
using Aizen.Modules.ReferenceData.Repository.Seed.Readers;
using Aizen.Modules.ReferenceData.Repository.Seed.Services;
using Aizen.Modules.ReferenceData.Repository.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;

namespace Aizen.Modules.ReferenceData.Repository;

public static class DependencyInjection
{
    public static IServiceCollection AddReferenceDataRepository(
        this IServiceCollection services,
        IConfiguration configuration)
    {

        services.AddScoped<ICurrencyRepository, CurrencyRepository>();
        services.AddScoped<IExchangeRateRepository, ExchangeRateRepository>();
        services.AddScoped<ILookupGroupRepository, LookupGroupRepository>();
        services.AddScoped<ILookupItemRepository, LookupItemRepository>();
        services.AddScoped<IMeasurementUnitRepository, MeasurementUnitRepository>();
        services.AddScoped<ISystemParameterRepository, SystemParameterRepository>();
        services.AddScoped<ILocationRepository, LocationRepository>();
        services.AddScoped<ReferenceDataMongoIndexInitializer>(sp =>
        {
            var mongoContext = sp.GetRequiredService<ReferenceDataMongoDbContext>();
            return new ReferenceDataMongoIndexInitializer(mongoContext);
        });

        // JSON Seed infrastructure
        services.Configure<ReferenceDataSeedOptions>(configuration.GetSection("ReferenceDataSeed"));
        services.AddScoped<IReferenceDataJsonSeedReader, ReferenceDataJsonSeedReader>();

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
        services.AddSingleton<IReferenceDataCacheKeyService, ReferenceDataCacheKeyService>();
        services.AddScoped<IReferenceDataCacheInvalidationService, ReferenceDataCacheInvalidationService>();
        services.AddScoped<IReferenceDataMongoIndexService, ReferenceDataMongoIndexService>();
        services.AddScoped<IReferenceDataSeedService, ReferenceDataSeedService>();

        // JSON Seed services
        services.AddScoped<CurrencyJsonSeedService>();
        services.AddScoped<ExchangeRateJsonSeedService>();
        services.AddScoped<MeasurementJsonSeedService>();
        services.AddScoped<LookupJsonSeedService>();
        services.AddScoped<SystemJsonSeedService>();
        services.AddScoped<LocationJsonSeedService>();
        services.AddScoped<LocationSlugBackfillService>();
        services.AddScoped<IReferenceDataJsonSeedService, ReferenceDataJsonSeedService>();

        return services;
    }

    public static async Task SeedReferenceDataAsync(this IHost host, CancellationToken ct = default)
    {
        using var scope = host.Services.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<ReferenceDataDbContext>();
        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync(ct);
        if (pendingMigrations.Any())
            await dbContext.Database.MigrateAsync(ct);

        var mongoIndexService = scope.ServiceProvider.GetRequiredService<IReferenceDataMongoIndexService>();
        await mongoIndexService.EnsureIndexesAsync(ct);

        var seedService = scope.ServiceProvider.GetRequiredService<IReferenceDataJsonSeedService>();
        await seedService.SeedAllAsync(ct);
    }
}
