using Aizen.Core.Common.Abstraction.Helpers;
using Aizen.Core.Common.Abstraction.Settings;
using Aizen.Core.Infrastructure.RemoteCall;
using Aizen.Core.Infrastructure.RemoteCall.Serialization;
using Aizen.Core.RemoteCall.Abstraction;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Aizen.Core.Configuration.Extensions;
using Refit;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Aizen.Core.RemoteCall.Extensions;

public static class BuilderExtensions
{
    /// <summary>
    /// Shared Refit settings with a custom <see cref="SystemTextJsonContentSerializer"/> that
    /// includes the <see cref="PaginateJsonConverterFactory"/> so <c>Paginate&lt;T&gt;</c>
    /// (which has only an internal parameterless constructor) can be deserialized by Refit clients.
    /// </summary>
    public static readonly RefitSettings AizenRefitSettings = new(
        new SystemTextJsonContentSerializer(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters =
            {
                new JsonStringEnumConverter(),   // handles string enums from Newtonsoft API responses
                new PaginateJsonConverterFactory()
            }
        }));

    // Keep internal alias for the extension methods below.
    private static readonly RefitSettings _refitSettings = AizenRefitSettings;

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
                    var remoteCallClient = RestService.For(typeToRegister, httpClient, _refitSettings);

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
                    var remoteCallClient = RestService.For(typeToRegister, httpClient, _refitSettings);

                    return remoteCallClient;
                });
            }
        }

        return services;
    }
}