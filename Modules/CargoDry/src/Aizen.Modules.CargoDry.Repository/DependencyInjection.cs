using Aizen.Modules.CargoDry.Domain.Interface.Repository;
using Aizen.Modules.CargoDry.Repository.Persistence;
using Aizen.Modules.CargoDry.Repository.Repositories;
using Aizen.Modules.CargoDry.Repository.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aizen.Modules.CargoDry.Repository;

public static class DependencyInjection
{
    public static IServiceCollection AddCargoDryRepository(this IServiceCollection services)
    {
        services.AddScoped<ICargoDryProductRepository, CargoDryProductRepository>();
        services.AddScoped<ICargoDryKitRepository,     CargoDryKitRepository>();
        services.AddScoped<ICargoDryBatchRepository,   CargoDryBatchRepository>();
        services.AddScoped<CargoDryProductSeed>();
        return services;
    }

    public static async Task SeedCargoDryAsync(this IHost host, CancellationToken ct = default)
    {
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CargoDryDbContext>();
        var pending = await db.Database.GetPendingMigrationsAsync(ct);
        if (pending.Any()) await db.Database.MigrateAsync(ct);
        var seeder = scope.ServiceProvider.GetRequiredService<CargoDryProductSeed>();
        await seeder.SeedAsync(ct);
    }
}
