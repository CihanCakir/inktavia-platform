using Aizen.Modules.Vessel.Domain.Interface.Repository;
using Aizen.Modules.Vessel.Domain.Interface.Service;
using Aizen.Modules.Vessel.Repository.Mongo;
using Aizen.Modules.Vessel.Repository.Persistence;
using Aizen.Modules.Vessel.Repository.Repositories;
using Aizen.Modules.Vessel.Repository.Service;
using Aizen.Modules.Vessel.Repository.Service.FileStorage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aizen.Modules.Vessel.Repository;

public static class DependencyInjection
{
    public static IServiceCollection AddVesselRepository(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IVesselRepository, VesselRepository>();
        services.AddScoped<IVesselOwnerRepository, VesselOwnerRepository>();
        services.AddScoped<IVesselSpecificationRepository, VesselSpecificationRepository>();
        services.AddScoped<IVesselEngineRepository, VesselEngineRepository>();
        services.AddScoped<IVesselDocumentRepository, VesselDocumentRepository>();
        services.AddScoped<IVesselMediaRepository, VesselMediaRepository>();
        services.AddScoped<IVesselLocationSnapshotRepository, VesselLocationSnapshotRepository>();
        services.AddScoped<IVesselStatusHistoryRepository, VesselStatusHistoryRepository>();

        services.AddScoped<VesselProfileReadRepository>();
        services.AddScoped<VesselMongoIndexInitializer>(sp =>
        {
            var mongoContext = sp.GetRequiredService<VesselMongoDbContext>();
            return new VesselMongoIndexInitializer(mongoContext);
        });

        return services;
    }

    public static IServiceCollection AddVesselServices(this IServiceCollection services)
    {
        services.AddScoped<IVesselReferenceValidationService, VesselReferenceValidationService>();
        services.AddScoped<IVesselAccessService, VesselAccessService>();
        services.AddScoped<IVesselOwnershipService, VesselOwnershipService>();
        services.AddScoped<IVesselStatusService, VesselStatusService>();
        services.AddScoped<IVesselSnapshotService, VesselSnapshotService>();
        services.AddSingleton<IVesselCacheKeyService, VesselCacheKeyService>();
        services.AddScoped<IVesselCacheInvalidationService, VesselCacheInvalidationService>();
        services.AddScoped<IVesselFileStorageService, VesselFileStorageService>();

        return services;
    }

    public static async Task SeedVesselAsync(this IHost host, CancellationToken ct = default)
    {
        using var scope = host.Services.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<VesselDbContext>();
        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync(ct);
        if (pendingMigrations.Any())
            await dbContext.Database.MigrateAsync(ct);

        var mongoIndexInitializer = scope.ServiceProvider.GetRequiredService<VesselMongoIndexInitializer>();
        await mongoIndexInitializer.InitializeAsync(ct);
    }
}
