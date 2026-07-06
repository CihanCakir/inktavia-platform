using Aizen.Modules.Profile.Domain.Interface.Repository;
using Aizen.Modules.Profile.Repository.Persistence;
using Aizen.Modules.Profile.Repository.Repositories;
using Aizen.Modules.Profile.Repository.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aizen.Modules.Profile.Repository;

public static class DependencyInjection
{
    public static IServiceCollection AddProfileRepository(
        this IServiceCollection services, IConfiguration configuration)
    {
        // ── Performance repositories ──────────────────────────────────────────
        services.AddScoped<IProfilePerformanceSnapshotRepository, ProfilePerformanceSnapshotRepository>();
        services.AddScoped<IProfileScoreComponentRepository,      ProfileScoreComponentRepository>();
        services.AddScoped<IProfileScoreHistoryRepository,        ProfileScoreHistoryRepository>();
        services.AddScoped<IProfileDecisionLogRepository,         ProfileDecisionLogRepository>();
        services.AddScoped<IProfileRiskSignalRepository,          ProfileRiskSignalRepository>();
        services.AddScoped<IProfileMetricCacheRepository,         ProfileMetricCacheRepository>();

        // ── Seed classes ──────────────────────────────────────────────────────
        services.AddScoped<ProfilePerformanceMockSeed>();

        return services;
    }

    public static async Task SeedProfileAsync(this IHost host, CancellationToken ct = default)
    {
        using var scope = host.Services.CreateScope();

        var db      = scope.ServiceProvider.GetRequiredService<ProfileDbContext>();
        var pending = await db.Database.GetPendingMigrationsAsync(ct);
        if (pending.Any()) await db.Database.MigrateAsync(ct);

        // ── Performance mock data ─────────────────────────────────────────────
        var performanceSeed = scope.ServiceProvider.GetRequiredService<ProfilePerformanceMockSeed>();
        await performanceSeed.SeedAsync(ct);
    }
}
