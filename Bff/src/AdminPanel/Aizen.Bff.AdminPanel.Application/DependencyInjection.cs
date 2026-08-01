using Aizen.Bff.AdminPanel.Application.Common.Http;
using Aizen.Bff.AdminPanel.Application.Common.Options;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Services;
using Aizen.Core.Cache.Extension;
using Aizen.Core.RemoteCall.Abstraction;
using Aizen.Core.RemoteCall.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Refit;
using RemoteCallBuilderExtensions = Aizen.Core.RemoteCall.Extensions.BuilderExtensions;

namespace Aizen.Bff.AdminPanel.Application;

[DocumentationInfo("Admin Panel BFF DI registration",
    "Registers BFF remote call interfaces and application-layer services. " +
    "All downstream HTTP calls are routed through AdminPanelBffAuthDelegatingHandler which " +
    "injects Authorization (Keycloak service token) and, when configured, the trusted-BFF identity assertion " +
    "(X-Aizen-Bff-Assertion + X-Aizen-User-Id from IAdminIdentityHolder). The user JWT is never forwarded.")]
public static class DependencyInjection
{
    public static IServiceCollection AddAdminPanelBffApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddAizenCache(configuration);

        services.AddHttpContextAccessor();

        services.AddOptions<KeycloakServiceTokenOptions>()
            .Bind(configuration.GetSection(KeycloakServiceTokenOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // AdminPanelKeycloak options — supplies the module-assertion shared secret to the outgoing auth handler.
        services.AddOptions<AdminPanelKeycloakOptions>()
            .Bind(configuration.GetSection(AdminPanelKeycloakOptions.SectionName));

        services.AddScoped<IAdminPanelBffKeycloakServiceTokenProvider, AdminPanelBffKeycloakServiceTokenProvider>();

        // Per-request admin identity (UserId only) + by-subject resolver — mirrors the provider's
        // IProviderContext/IProviderIdentityHolder/ProviderProfileResolver, used to assert the acting admin to modules.
        services.AddScoped<IAdminContext, AdminContext>();
        services.AddScoped<IAdminIdentityHolder, AdminIdentityHolder>();
        services.AddScoped<IAdminIdentityResolver, AdminIdentityResolver>();

        // Central auth handler — wired into every downstream HttpClient.
        services.AddTransient<AdminPanelBffAuthDelegatingHandler>();

        // Fail-envelope fidelity handler — wired ONLY into the Payment remote client (FIX_RULE_CONFLICT_ENVELOPE):
        // converts a Payment-module fail envelope into an AizenBusinessException so *RuleConflict/*Invalid reach the
        // FE as a structured 400 (typed conflict banner) instead of a generic 500. Not used by any other module client.
        services.AddTransient<AdminPaymentBffFailEnvelopeHandler>();

        // ── Remote call registrations ──────────────────────────────────────────

        services.AddTransient<IIdentityAdminBffRemoteCall>(provider =>
            CreateRemoteCall<IIdentityAdminBffRemoteCall>(
                CreateHttpClient(provider, nameof(IIdentityAdminBffRemoteCall))));

        services.AddTransient<IVesselAdminBffRemoteCall>(provider =>
            CreateRemoteCall<IVesselAdminBffRemoteCall>(
                CreateHttpClient(provider, nameof(IVesselAdminBffRemoteCall))));

        services.AddTransient<IFileStorageAdminBffRemoteCall>(provider =>
            CreateRemoteCall<IFileStorageAdminBffRemoteCall>(
                CreateHttpClient(provider, nameof(IFileStorageAdminBffRemoteCall))));

        services.AddTransient<IServiceRequestAdminBffRemoteCall>(provider =>
            CreateRemoteCall<IServiceRequestAdminBffRemoteCall>(
                CreateHttpClient(provider, nameof(IServiceRequestAdminBffRemoteCall))));

        services.AddTransient<IReferenceDataAdminBffRemoteCall>(provider =>
            CreateRemoteCall<IReferenceDataAdminBffRemoteCall>(
                CreateHttpClient(provider, nameof(IReferenceDataAdminBffRemoteCall))));

        services.AddTransient<IAdminCargoDryBffRemoteCall>(provider =>
            CreateRemoteCall<IAdminCargoDryBffRemoteCall>(
                CreateHttpClient(provider, nameof(IAdminCargoDryBffRemoteCall))));

        services.AddTransient<INotificationBffRemoteCall>(provider =>
            CreateRemoteCall<INotificationBffRemoteCall>(
                CreateHttpClient(provider, nameof(INotificationBffRemoteCall))));

        services.AddTransient<IAdminMessagingBffRemoteCall>(provider =>
            CreateRemoteCall<IAdminMessagingBffRemoteCall>(
                CreateHttpClient(provider, nameof(IAdminMessagingBffRemoteCall))));

        services.AddTransient<INotificationAdminBffRemoteCall>(provider =>
            // Key matches docker-compose: RemoteCalls__IAdminNotificationBffRemoteCall__BaseUrl
            CreateRemoteCall<INotificationAdminBffRemoteCall>(
                CreateHttpClient(provider, "IAdminNotificationBffRemoteCall")));

        services.AddTransient<IAdminPaymentBffRemoteCall>(provider =>
            CreateRemoteCall<IAdminPaymentBffRemoteCall>(
                CreateHttpClient(provider, nameof(IAdminPaymentBffRemoteCall),
                    innerHandler: provider.GetRequiredService<AdminPaymentBffFailEnvelopeHandler>())));

        services.AddTransient<IAdminProfilePerformanceBffRemoteCall>(provider =>
            CreateRemoteCall<IAdminProfilePerformanceBffRemoteCall>(
                CreateHttpClient(provider, nameof(IAdminProfilePerformanceBffRemoteCall))));

        return services;
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds an HttpClient with <see cref="AdminPanelBffAuthDelegatingHandler"/> as the outermost handler and
    /// <see cref="HttpClientHandler"/> as the inner transport. When <paramref name="innerHandler"/> is supplied it is
    /// spliced between the auth handler and the transport (auth → inner → transport) — used by the Payment client to
    /// carry the fail-envelope fidelity handler. BaseAddress is resolved from <see cref="RemoteCallConfigurations"/>.
    /// </summary>
    private static HttpClient CreateHttpClient(
        IServiceProvider provider, string clientName, DelegatingHandler? innerHandler = null)
    {
        var authHandler = provider.GetRequiredService<AdminPanelBffAuthDelegatingHandler>();

        if (innerHandler is not null)
        {
            innerHandler.InnerHandler = new HttpClientHandler();
            authHandler.InnerHandler = innerHandler;
        }
        else
        {
            authHandler.InnerHandler = new HttpClientHandler();
        }

        var configs = provider.GetRequiredService<IOptions<RemoteCallConfigurations>>().Value;
        configs.TryGetValue(clientName, out var cfg);

        var client = new HttpClient(authHandler);
        if (cfg?.BaseUrl is not null)
            client.BaseAddress = new Uri(cfg.BaseUrl);

        return client;
    }

    private static T CreateRemoteCall<T>(HttpClient client) where T : class
        => RestService.For<T>(client, RemoteCallBuilderExtensions.AizenRefitSettings);
}
