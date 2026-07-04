using Aizen.Modules.CargoDry.Abstraction.Interface.Service;
using Aizen.Modules.CargoDry.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Aizen.Modules.CargoDry.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddCargoDryApplication(this IServiceCollection services)
    {
        services.AddScoped<ICargoDryQrService,                   CargoDryQrService>();
        services.AddScoped<IActivationTokenService,              ActivationTokenService>();
        services.AddScoped<IBatchKeyVaultService,               BatchKeyVaultService>();
        services.AddScoped<ICargoDryCommercialActivationService, CargoDryCommercialActivationService>();

        // Phase 5: Commercial rule resolver
        services.AddScoped<ICargoDryCommercialRuleResolver,      CargoDryCommercialRuleResolver>();

        // Recurring jobs are registered via AddAizenRecurringJob() in Program.cs
        // which auto-discovers IAizenRecurringJob implementations through assembly scanning.

        return services;
    }
}
