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
    "automatically injects Authorization (Keycloak service token) and X-Aizen-User-Token " +
    "(identity JWT from IAizenUserInfoAccessor). AuthorizationForwardingHandler is no longer used.")]
public static class DependencyInjection
{
    public static IServiceCollection AddAdminPanelBffApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddAizenCache(configuration);

        services.AddOptions<KeycloakServiceTokenOptions>()
            .Bind(configuration.GetSection(KeycloakServiceTokenOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddScoped<IAdminPanelBffKeycloakServiceTokenProvider, AdminPanelBffKeycloakServiceTokenProvider>();

        // Central auth handler — wired into every downstream HttpClient.
        services.AddTransient<AdminPanelBffAuthDelegatingHandler>();

        // ── Remote call registrations ──────────────────────────────────────────

        services.AddTransient<IIdentityAdminBffRemoteCall>(provider =>
        {
            var client = CreateHttpClient(provider, nameof(IIdentityAdminBffRemoteCall));
            client.Timeout = TimeSpan.FromSeconds(15);
            return CreateRemoteCall<IIdentityAdminBffRemoteCall>(client);
        });

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
                CreateHttpClient(provider, nameof(IAdminPaymentBffRemoteCall))));

        services.AddTransient<IAdminProfilePerformanceBffRemoteCall>(provider =>
            CreateRemoteCall<IAdminProfilePerformanceBffRemoteCall>(
                CreateHttpClient(provider, nameof(IAdminProfilePerformanceBffRemoteCall))));

        return services;
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds an HttpClient with <see cref="AdminPanelBffAuthDelegatingHandler"/> as the outermost
    /// handler and <see cref="HttpClientHandler"/> as the inner transport.
    /// BaseAddress is resolved from <see cref="RemoteCallConfigurations"/>.
    /// </summary>
    private static HttpClient CreateHttpClient(IServiceProvider provider, string clientName)
    {
        var authHandler = provider.GetRequiredService<AdminPanelBffAuthDelegatingHandler>();
        authHandler.InnerHandler = new HttpClientHandler();

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
