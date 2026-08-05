using Aizen.Modules.Notification.Domain.Interface.Repository;
using Aizen.Modules.Notification.Repository.Persistence;
using Aizen.Modules.Notification.Repository.Repositories;
using Aizen.Modules.Notification.Repository.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aizen.Modules.Notification.Repository;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationRepository(this IServiceCollection services)
    {
        services.AddScoped<INotificationRepository,         NotificationRepository>();
        services.AddScoped<INotificationTemplateRepository, NotificationTemplateRepository>();
        services.AddScoped<IUserDeviceTokenRepository,      UserDeviceTokenRepository>();
        services.AddScoped<INotificationPreferenceRepository, NotificationPreferenceRepository>();
        services.AddScoped<NotificationTemplateSeed>();
        services.AddScoped<CargoDryProviderMilestoneMockSeed>();
        services.AddScoped<NotificationDisplayMockSeed>();
        return services;
    }

    public static async Task SeedNotificationAsync(this IHost host, CancellationToken ct = default)
    {
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();

        var pending = await db.Database.GetPendingMigrationsAsync(ct);
        if (pending.Any())
            await db.Database.MigrateAsync(ct);

        var seeder = scope.ServiceProvider.GetRequiredService<NotificationTemplateSeed>();
        await seeder.SeedAsync(ct);

        // Dev/local mock milestone notifications (idempotent, env-gated)
        var env = scope.ServiceProvider.GetService<IHostEnvironment>();
        if (env is null || env.IsDevelopment())
        {
            var milestoneMock = scope.ServiceProvider.GetRequiredService<CargoDryProviderMilestoneMockSeed>();
            await milestoneMock.SeedAsync(ct);

            var displayMock = scope.ServiceProvider.GetRequiredService<NotificationDisplayMockSeed>();
            await displayMock.SeedAsync(ct);
        }
    }
}
