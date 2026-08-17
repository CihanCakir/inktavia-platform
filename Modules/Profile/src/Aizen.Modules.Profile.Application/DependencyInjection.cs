using Aizen.Modules.Profile.Abstraction.Interface.Service;
using Aizen.Modules.Profile.Application.Performance;
using Microsoft.Extensions.DependencyInjection;

namespace Aizen.Modules.Profile.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddProfileApplication(this IServiceCollection services)
    {
        // ── Performance sub-module services ──────────────────────────────────
        // ProfilePerformanceEngine performs cross-schema ADO.NET reads.
        // Registered as Scoped so it shares the ProfileDbContext lifetime.
        services.AddScoped<IProfilePerformanceEngine, ProfilePerformanceEngine>();

        return services;
    }
}
