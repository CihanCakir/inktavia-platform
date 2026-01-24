using Aizen.Core.Common.Abstraction.Helpers;
using Aizen.Core.Common.Abstraction.Settings;
using Aizen.Core.Infrastructure.RemoteCall;
using Aizen.Core.RemoteCall.Abstraction;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Aizen.Core.Configuration.Extentions;
using Refit;

namespace Aizen.Core.RemoteCall.Extentions;

public static class BuilderExtensions
{
    public static IServiceCollection AddAizenRemoteCall(this IServiceCollection services, IConfiguration configuration)
    {
        services.ConfigureDictionary<RemoteCallConfigurations, RemoteCallConfiguration>(
            configuration.GetSection($"RemoteCalls"));

        services.AddScoped<IHttpClientFactory, AizenHttpClientFactory>();

        foreach (var abstractionAssembly in AizenModuleAssemblyDiscovery.GetInstance().AbstractionAssemblies)
        {
            foreach (var typeToRegister in abstractionAssembly.GetTypes().Where(x => typeof(IAizenRemoteCall).IsAssignableFrom(x) && x.IsInterface))
            {
                services.AddTransient(typeToRegister, provider =>
                {
                    var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
                    var httpClient = httpClientFactory.CreateClient(typeToRegister.Name);
                    var remoteCallClient = RestService.For(typeToRegister, httpClient);

                    return remoteCallClient;
                });
            }
        }

        foreach (var abstractionAssembly in AizenModuleAssemblyDiscovery.GetInstance().CoreAssemblies)
        {
            foreach (var typeToRegister in abstractionAssembly.GetTypes().Where(x => typeof(IAizenRemoteCall).IsAssignableFrom(x) && x.IsInterface))
            {
                services.AddTransient(typeToRegister, provider =>
                {
                    var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
                    var httpClient = httpClientFactory.CreateClient(typeToRegister.Name);
                    var remoteCallClient = RestService.For(typeToRegister, httpClient);

                    return remoteCallClient;
                });
            }
        }

        return services;
    }
}