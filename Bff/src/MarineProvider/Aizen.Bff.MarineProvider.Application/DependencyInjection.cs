using Aizen.Bff.MarineProvider.Application.Common.Http;
using Aizen.Bff.MarineProvider.Application.Common.Options;
using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Core.RemoteCall.Abstraction;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Refit;
using RemoteCallBuilderExtensions = Aizen.Core.RemoteCall.Extensions.BuilderExtensions;

namespace Aizen.Bff.MarineProvider.Application;

/// <summary>
/// MarineProvider BFF application-layer DI.
/// Registers Keycloak options, the Keycloak service-token provider + Admin API client,
/// the provider context resolver, the outgoing auth handler, and Refit remote calls to modules.
/// CQRS handlers are discovered by the BFF starter (AddAizenCQRS) from this assembly.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddMarineProviderBffApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddMemoryCache();
        services.AddHttpClient();

        services.AddOptions<MarineProviderKeycloakOptions>()
            .Bind(configuration.GetSection(MarineProviderKeycloakOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddScoped<IProviderContext, ProviderContext>();
        services.AddScoped<IProviderIdentityHolder, ProviderIdentityHolder>();
        services.AddScoped<IProviderProfileResolver, ProviderProfileResolver>();
        services.AddSingleton<IProviderKeycloakServiceTokenProvider, ProviderKeycloakServiceTokenProvider>();
        services.AddScoped<IProviderKeycloakAdminClient, ProviderKeycloakAdminClient>();

        // Central outgoing auth handler wired into every downstream Refit client.
        services.AddTransient<MarineProviderBffAuthDelegatingHandler>();

        services.AddTransient<IIdentityRemoteCall>(provider =>
            CreateRemoteCall<IIdentityRemoteCall>(
                CreateHttpClient(provider, nameof(IIdentityRemoteCall))));

        services.AddTransient<IServiceRequestRemoteCall>(provider =>
            CreateRemoteCall<IServiceRequestRemoteCall>(
                CreateHttpClient(provider, nameof(IServiceRequestRemoteCall))));

        services.AddTransient<IFileStorageRemoteCall>(provider =>
            CreateRemoteCall<IFileStorageRemoteCall>(
                CreateHttpClient(provider, nameof(IFileStorageRemoteCall))));

        services.AddTransient<INotificationRemoteCall>(provider =>
            CreateRemoteCall<INotificationRemoteCall>(
                CreateHttpClient(provider, nameof(INotificationRemoteCall))));

        services.AddTransient<IReferenceDataRemoteCall>(provider =>
            CreateRemoteCall<IReferenceDataRemoteCall>(
                CreateHttpClient(provider, nameof(IReferenceDataRemoteCall))));

        services.AddTransient<IVesselRemoteCall>(provider =>
            CreateRemoteCall<IVesselRemoteCall>(
                CreateHttpClient(provider, nameof(IVesselRemoteCall))));

        services.AddTransient<ICargoDryRemoteCall>(provider =>
            CreateRemoteCall<ICargoDryRemoteCall>(
                CreateHttpClient(provider, nameof(ICargoDryRemoteCall))));

        return services;
    }

    private static HttpClient CreateHttpClient(IServiceProvider provider, string clientName)
    {
        var authHandler = provider.GetRequiredService<MarineProviderBffAuthDelegatingHandler>();
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
