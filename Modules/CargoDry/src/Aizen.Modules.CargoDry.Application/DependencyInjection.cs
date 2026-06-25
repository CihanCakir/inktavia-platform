using Aizen.Modules.CargoDry.Abstraction.Interface.Service;
using Aizen.Modules.CargoDry.Application.Jobs;
using Aizen.Modules.CargoDry.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Aizen.Modules.CargoDry.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddCargoDryApplication(this IServiceCollection services)
    {
        services.AddScoped<ICargoDryQrService,      CargoDryQrService>();
        services.AddScoped<IActivationTokenService, ActivationTokenService>();
        services.AddScoped<IBatchKeyVaultService,   BatchKeyVaultService>();

        services.AddHostedService<KitExpiryReminderJob>();
        services.AddHostedService<KitExpiredMarkingJob>();
        services.AddHostedService<DailySnapshotJob>();

        return services;
    }
}
