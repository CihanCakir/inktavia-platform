using Aizen.Modules.CargoDry.Domain.Interface.Repository;
using Aizen.Modules.CargoDry.Repository.Persistence;
using Aizen.Modules.CargoDry.Repository.Repositories;
using Aizen.Modules.CargoDry.Repository.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aizen.Modules.CargoDry.Repository;

public static class DependencyInjection
{
    public static IServiceCollection AddCargoDryRepository(
        this IServiceCollection services, IConfiguration configuration)
    {
        // ── PostgreSQL ─────────────────────────────────────────────────────────
        services.AddScoped<ICargoDryProductRepository,              CargoDryProductRepository>();
        services.AddScoped<ICargoDryKitRepository,                  CargoDryKitRepository>();
        services.AddScoped<ICargoDryBatchRepository,                CargoDryBatchRepository>();
        services.AddScoped<ICargoDryConsignmentAgreementRepository, CargoDryConsignmentAgreementRepository>();
        services.AddScoped<CargoDryProductSeed>();
        services.AddScoped<CargoDryBatchMockSeed>();

        // ── MongoDB ────────────────────────────────────────────────────────────
        // IMongoClient and IMongoDatabase are NOT registered here.
        // AddAizenMongo() in Program.cs auto-discovers CargoDryMongoDbContext.
        services.AddScoped<ICargoDryActivationLogRepository, CargoDryActivationLogRepository>();
        services.AddScoped<ICargoDrySnapshotRepository,      CargoDrySnapshotRepository>();
        services.AddScoped<CargoDryMongoIndexInitializer>();

        return services;
    }

    public static async Task SeedCargoDryAsync(this IHost host, CancellationToken ct = default)
    {
        using var scope = host.Services.CreateScope();

        var db      = scope.ServiceProvider.GetRequiredService<CargoDryDbContext>();
        var pending = await db.Database.GetPendingMigrationsAsync(ct);
        if (pending.Any()) await db.Database.MigrateAsync(ct);
        var productSeeder = scope.ServiceProvider.GetRequiredService<CargoDryProductSeed>();
        await productSeeder.SeedAsync(ct);

        var batchSeeder = scope.ServiceProvider.GetRequiredService<CargoDryBatchMockSeed>();
        await batchSeeder.SeedAsync(ct);

        var mongoIndexer = scope.ServiceProvider.GetRequiredService<CargoDryMongoIndexInitializer>();
        await mongoIndexer.InitializeAsync(ct);
    }
}
