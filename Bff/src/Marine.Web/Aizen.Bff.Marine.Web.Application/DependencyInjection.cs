using Aizen.Bff.Marine.Web.Application.Common.Http;
using Aizen.Bff.Marine.Web.Application.Common.Options;
using Aizen.Bff.Marine.Web.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Web.Application.Common.Services;
using Aizen.Core.RemoteCall.Abstraction;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Refit;
using RemoteCallBuilderExtensions = Aizen.Core.RemoteCall.Extensions.BuilderExtensions;

namespace Aizen.Bff.Marine.Web.Application;

/// <summary>
/// Marine Web BFF application-layer DI (Foundation / Phase W0).
/// Registers Keycloak options, the Keycloak service-token provider, the participant context + identity holder,
/// and the outgoing auth handler. The feature remote clients (Content, ReferenceData, Identity, …) and the
/// participant-profile resolver land in later phases. CQRS handlers are discovered by the BFF starter
/// (AddAizenCQRS) from this assembly.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddMarineWebBffApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddMemoryCache();
        services.AddHttpClient();

        services.AddOptions<MarineWebKeycloakOptions>()
            .Bind(configuration.GetSection(MarineWebKeycloakOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddScoped<IWebParticipantContext, WebParticipantContext>();
        services.AddScoped<IWebIdentityHolder, WebIdentityHolder>();
        services.AddScoped<IWebParticipantProfileResolver, WebParticipantProfileResolver>();
        services.AddSingleton<IWebKeycloakServiceTokenProvider, WebKeycloakServiceTokenProvider>();

        // Central outgoing auth handler wired into every downstream Refit client.
        services.AddTransient<MarineWebBffAuthDelegatingHandler>();

        // W1/W2 — Content module client: public read (raw DTOs) + participant /me engagement (enveloped). Base URL
        // from RemoteCalls:IContentRemoteCall:BaseUrl (→ the content-api host).
        services.AddTransient<IContentRemoteCall>(provider =>
            CreateRemoteCall<IContentRemoteCall>(
                CreateHttpClient(provider, nameof(IContentRemoteCall))));

        // W2 — Identity module client (participant profile by Keycloak subject). Base URL from
        // RemoteCalls:IIdentityRemoteCall:BaseUrl (→ the identity-api host).
        services.AddTransient<IIdentityRemoteCall>(provider =>
            CreateRemoteCall<IIdentityRemoteCall>(
                CreateHttpClient(provider, nameof(IIdentityRemoteCall))));

        // W3 — ReferenceData module client (public countries / cities / lookup-items). Base URL from
        // RemoteCalls:IReferenceDataRemoteCall:BaseUrl (→ the reference-data-api host).
        services.AddTransient<IReferenceDataRemoteCall>(provider =>
            CreateRemoteCall<IReferenceDataRemoteCall>(
                CreateHttpClient(provider, nameof(IReferenceDataRemoteCall))));

        return services;
    }

    private static HttpClient CreateHttpClient(IServiceProvider provider, string clientName)
    {
        var authHandler = provider.GetRequiredService<MarineWebBffAuthDelegatingHandler>();
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
