using Aizen.Modules.CargoDry.Abstraction.Interface.Service;
using Aizen.Modules.CargoDry.Application.Services;
using Aizen.Modules.Payment.Abstraction.Interface;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
        // Split-host fallback: aizen-cargodry runs without Payment.Application, whose DI provides the real
        // ICargoDryCommissionRuleLookupService. TryAdd keeps the Payment implementation authoritative when
        // co-hosted; standalone, the null-object degrades the resolver to agreement-rate tiers (Supply v2 fix).
        services.TryAddScoped<ICargoDryCommissionRuleLookupService, NullCargoDryCommissionRuleLookupService>();

        // CE-6c: Milestone evaluator
        services.AddScoped<ICargoDryProviderMilestoneEvaluator, CargoDryProviderMilestoneEvaluator>();

        // Phase 6: Monthly settlement automation
        services.AddScoped<ICargoDryMonthlySettlementAutomationService, CargoDryMonthlySettlementAutomationService>();

        // Recurring jobs are registered via AddAizenRecurringJob() in Program.cs
        // which auto-discovers IAizenRecurringJob implementations through assembly scanning.

        return services;
    }
}
