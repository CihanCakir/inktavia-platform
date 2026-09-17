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

        // ── Split-host Payment bridges ────────────────────────────────────────────
        // Every service below is implemented in Payment.Application (in-process MediatR bridge) and injected by a
        // CargoDry settlement/invoice handler. Co-hosted, Payment.Application's real AddScoped wins over these TryAdds.
        // In the split aizen-cargodry pod (no Payment.Application) these registrations are what let the handlers ACTIVATE
        // at all — without them the module 911s on DI activation (this task's bug class). See the DI smoke test.
        //
        // All four bridges are real HTTP bridges over the single ICargoDrySettlementPaymentRemoteCall client (one BaseUrl:
        // RemoteCalls__ICargoDrySettlementPaymentRemoteCall__BaseUrl; AddAizenRemoteCall is wired by the Operation Starter).
        // Co-hosted, Payment.Application's in-process AddScoped wins over these TryAdds; the split aizen-cargodry pod uses
        // the remote impls. Complete only moves the PayoutRecord — settlement closure (→ Settled) stays in the CargoDry
        // CompleteCargoDrySettlementPayout handler.
        services.TryAddScoped<ICargoDrySettlementPayoutService, RemoteCargoDrySettlementPayoutService>();
        services.TryAddScoped<ICargoDrySettlementPayoutLifecycleService, RemoteCargoDrySettlementPayoutLifecycleService>();
        services.TryAddScoped<ICargoDrySettlementInvoiceService, RemoteCargoDrySettlementInvoiceService>();
        services.TryAddScoped<ICargoDryRenewalInvoiceService, RemoteCargoDryRenewalInvoiceService>();

        // CE-6c: Milestone evaluator
        services.AddScoped<ICargoDryProviderMilestoneEvaluator, CargoDryProviderMilestoneEvaluator>();

        // Second-pass settlement linker (heals orphaned attributions) — used by the monthly automation.
        services.AddScoped<ICargoDrySettlementLinkingService, CargoDrySettlementLinkingService>();

        // Phase 6: Monthly settlement automation
        services.AddScoped<ICargoDryMonthlySettlementAutomationService, CargoDryMonthlySettlementAutomationService>();

        // Recurring jobs are registered via AddAizenRecurringJob() in Program.cs
        // which auto-discovers IAizenRecurringJob implementations through assembly scanning.

        return services;
    }
}
