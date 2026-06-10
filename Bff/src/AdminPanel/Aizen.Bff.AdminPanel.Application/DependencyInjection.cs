using Aizen.Bff.AdminPanel.Application.Common.Options;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Services;
using Aizen.Core.Cache.Extension;
using Aizen.Core.Infrastructure.RemoteCall;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

        services.AddTransient<IIdentityAdminBffRemoteCall>(provider =>
        {
            var factory = provider.GetRequiredService<IHttpClientFactory>();
            return RestService.For<IIdentityAdminBffRemoteCall>(
                factory.CreateClient(nameof(IIdentityAdminBffRemoteCall)));
        });

        services.AddTransient<IVesselAdminBffRemoteCall>(provider =>
        {
            var factory = provider.GetRequiredService<IHttpClientFactory>();
            return RestService.For<IVesselAdminBffRemoteCall>(
                factory.CreateClient(nameof(IVesselAdminBffRemoteCall)));
        });

        services.AddTransient<IFileStorageAdminBffRemoteCall>(provider =>
        {
            var factory = provider.GetRequiredService<IHttpClientFactory>();
            return RestService.For<IFileStorageAdminBffRemoteCall>(
                factory.CreateClient(nameof(IFileStorageAdminBffRemoteCall)));
        });

        services.AddTransient<IServiceRequestAdminBffRemoteCall>(provider =>
        {
            var factory = provider.GetRequiredService<IHttpClientFactory>();
            return RestService.For<IServiceRequestAdminBffRemoteCall>(
                factory.CreateClient(nameof(IServiceRequestAdminBffRemoteCall)));
        });

        services.AddTransient<IReferenceDataAdminBffRemoteCall>(provider =>
        {
            var factory = provider.GetRequiredService<IHttpClientFactory>();
            return RestService.For<IReferenceDataAdminBffRemoteCall>(
                factory.CreateClient(nameof(IReferenceDataAdminBffRemoteCall)));
        });

        return services;
    }
}
