using Aizen.Bff.AdminPanel.Application.Common.Http;
using Aizen.Bff.AdminPanel.Application.Common.Options;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Services;
using Aizen.Core.Cache.Extension;
using Aizen.Core.RemoteCall.Abstraction;
using Aizen.Core.RemoteCall.Extensions;
using RemoteCallBuilderExtensions = Aizen.Core.RemoteCall.Extensions.BuilderExtensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Refit;

namespace Aizen.Bff.AdminPanel.Application;

[DocumentationInfo("Admin Panel BFF DI registration", "Registers BFF-specific remote call interfaces and application-layer services. BFF remote call interfaces are not auto-discovered by AizenModuleAssemblyDiscovery (assembly name lacks 'Abstraction'), so they are registered manually here. Also registers Aizen Core/Cache (Redis) required by the Keycloak service-token provider.")]
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

        services.AddTransient<AuthorizationForwardingHandler>();

        services.AddTransient<IIdentityAdminBffRemoteCall>(provider =>
        {
            var factory = provider.GetRequiredService<IHttpClientFactory>();
            var httpClient = factory.CreateClient(nameof(IIdentityAdminBffRemoteCall));
            // Prevent identity remote calls from hanging indefinitely on wrong credentials or unreachable service.
            httpClient.Timeout = TimeSpan.FromSeconds(15);
            return RestService.For<IIdentityAdminBffRemoteCall>(httpClient, RemoteCallBuilderExtensions.AizenRefitSettings);
        });

        services.AddTransient<IVesselAdminBffRemoteCall>(provider =>
        {
            var factory = provider.GetRequiredService<IHttpClientFactory>();
            return RestService.For<IVesselAdminBffRemoteCall>(
                factory.CreateClient(nameof(IVesselAdminBffRemoteCall)),
                RemoteCallBuilderExtensions.AizenRefitSettings);
        });

        services.AddTransient<IFileStorageAdminBffRemoteCall>(provider =>
        {
            var factory = provider.GetRequiredService<IHttpClientFactory>();
            return RestService.For<IFileStorageAdminBffRemoteCall>(
                factory.CreateClient(nameof(IFileStorageAdminBffRemoteCall)),
                RemoteCallBuilderExtensions.AizenRefitSettings);
        });

        services.AddTransient<IServiceRequestAdminBffRemoteCall>(provider =>
        {
            var remoteCallConfigs = provider.GetRequiredService<IOptions<RemoteCallConfigurations>>().Value;
            remoteCallConfigs.TryGetValue(nameof(IServiceRequestAdminBffRemoteCall), out var srConfig);

            var forwardingHandler = provider.GetRequiredService<AuthorizationForwardingHandler>();
            forwardingHandler.InnerHandler = new HttpClientHandler();

            var httpClient = new HttpClient(forwardingHandler);
            if (srConfig?.BaseUrl is not null)
                httpClient.BaseAddress = new Uri(srConfig.BaseUrl);

            return RestService.For<IServiceRequestAdminBffRemoteCall>(
                httpClient,
                RemoteCallBuilderExtensions.AizenRefitSettings);
        });

        services.AddTransient<IReferenceDataAdminBffRemoteCall>(provider =>
        {
            var factory = provider.GetRequiredService<IHttpClientFactory>();
            return RestService.For<IReferenceDataAdminBffRemoteCall>(
                factory.CreateClient(nameof(IReferenceDataAdminBffRemoteCall)),
                RemoteCallBuilderExtensions.AizenRefitSettings);
        });

        return services;
    }
}
