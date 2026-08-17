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

        // Keycloak session refresh (AR2): the admin session is Keycloak-issued, so refresh goes to Keycloak's
        // refresh_token grant on the public admin-panel client — NOT the Identity UserLoginTokenEntity store.
        services.AddHttpClient();
        services.AddScoped<IAdminKeycloakAuthClient, AdminKeycloakAuthClient>();

        // Per-request admin identity (UserId only) + by-subject resolver — mirrors the provider's
        // IProviderContext/IProviderIdentityHolder/ProviderProfileResolver, used to assert the acting admin to modules.
        services.AddScoped<IAdminContext, AdminContext>();
        services.AddScoped<IAdminIdentityHolder, AdminIdentityHolder>();
        services.AddScoped<IAdminIdentityResolver, AdminIdentityResolver>();

        // Central auth handler — wired into every downstream HttpClient.
        services.AddTransient<AdminPanelBffAuthDelegatingHandler>();

        // Fail-envelope fidelity handler — wired into the module clients whose business errors the FE must read
        // verbatim: converts a module fail envelope into an AizenBusinessException so *RuleConflict/*Invalid (Payment)
        // and dispute-resolve outcomes (ServiceRequest) reach the FE as a structured 400 instead of a generic 500.
        services.AddTransient<AdminBffFailEnvelopeHandler>();

        // ── Remote call registrations ──────────────────────────────────────────
        // The HttpClient name passed to CreateHttpClient is the configuration key
        // (RemoteCalls__<key>__BaseUrl, also set via docker-compose env). It is an external
        // contract, so it stays byte-identical to the pre-refactor interface names even though
        // the interface types were renamed to the Admin-free I<Domain>RemoteCall convention.

        services.AddTransient<IIdentityRemoteCall>(provider =>
            CreateRemoteCall<IIdentityRemoteCall>(
                CreateHttpClient(provider, "IIdentityAdminBffRemoteCall")));

        services.AddTransient<IVesselRemoteCall>(provider =>
            CreateRemoteCall<IVesselRemoteCall>(
                CreateHttpClient(provider, "IVesselAdminBffRemoteCall")));

        services.AddTransient<IFileStorageRemoteCall>(provider =>
            CreateRemoteCall<IFileStorageRemoteCall>(
                CreateHttpClient(provider, "IFileStorageAdminBffRemoteCall")));

        services.AddTransient<IServiceRequestRemoteCall>(provider =>
            CreateRemoteCall<IServiceRequestRemoteCall>(
                CreateHttpClient(provider, "IServiceRequestAdminBffRemoteCall",
                    innerHandler: provider.GetRequiredService<AdminBffFailEnvelopeHandler>())));

        services.AddTransient<IReferenceDataRemoteCall>(provider =>
            CreateRemoteCall<IReferenceDataRemoteCall>(
                CreateHttpClient(provider, "IReferenceDataAdminBffRemoteCall")));

        services.AddTransient<ICargoDryRemoteCall>(provider =>
            CreateRemoteCall<ICargoDryRemoteCall>(
                CreateHttpClient(provider, "IAdminCargoDryBffRemoteCall")));

        services.AddTransient<INotificationRemoteCall>(provider =>
            CreateRemoteCall<INotificationRemoteCall>(
                CreateHttpClient(provider, "INotificationBffRemoteCall")));

        services.AddTransient<IMessagingRemoteCall>(provider =>
            CreateRemoteCall<IMessagingRemoteCall>(
                CreateHttpClient(provider, "IAdminMessagingBffRemoteCall")));

        services.AddTransient<INotificationTemplateRemoteCall>(provider =>
            // Key matches docker-compose: RemoteCalls__IAdminNotificationBffRemoteCall__BaseUrl
            CreateRemoteCall<INotificationTemplateRemoteCall>(
                CreateHttpClient(provider, "IAdminNotificationBffRemoteCall")));

        services.AddTransient<IPaymentRemoteCall>(provider =>
            CreateRemoteCall<IPaymentRemoteCall>(
                CreateHttpClient(provider, "IAdminPaymentBffRemoteCall",
                    innerHandler: provider.GetRequiredService<AdminBffFailEnvelopeHandler>())));

        services.AddTransient<IProfilePerformanceRemoteCall>(provider =>
            CreateRemoteCall<IProfilePerformanceRemoteCall>(
                CreateHttpClient(provider, "IAdminProfilePerformanceBffRemoteCall")));

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
