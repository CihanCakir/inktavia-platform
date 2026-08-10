using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Http;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Options;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Core.RemoteCall.Abstraction;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Refit;
using RemoteCallBuilderExtensions = Aizen.Core.RemoteCall.Extensions.BuilderExtensions;

namespace Aizen.Bff.Marine.Participant.Mobile.Application;

/// <summary>
/// Marine Participant Mobile BFF application-layer DI (Foundation / Phase 1).
/// Registers Keycloak options, the Keycloak service-token provider, the participant context + identity holder,
/// the outgoing auth handler, and the Foundation Refit remote calls (Identity + ReferenceData) as plumbing proof.
/// The full participant-profile resolver + Keycloak Admin client and the remaining feature remote clients are
/// added in later phases. CQRS handlers are discovered by the BFF starter (AddAizenCQRS) from this assembly.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddMarineMobileBffApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddMemoryCache();
        services.AddHttpClient();

        services.AddOptions<MarineMobileKeycloakOptions>()
            .Bind(configuration.GetSection(MarineMobileKeycloakOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddScoped<IParticipantContext, ParticipantContext>();
        services.AddScoped<IParticipantIdentityHolder, ParticipantIdentityHolder>();
        services.AddScoped<IParticipantProfileResolver, ParticipantProfileResolver>();
        // BE_MO2b — shared BFF-side provider profile-id → display-name resolver (batch, cached, cost-free).
        services.AddScoped<IProviderNameResolver, ProviderNameResolver>();
        services.AddSingleton<IParticipantKeycloakServiceTokenProvider, ParticipantKeycloakServiceTokenProvider>();

        // Native ticket→session handoff (OIDC auth-code + PKCE vs. the public inktavia-mobile client).
        // It constructs its own HttpClient with auto-redirect OFF so the BFF reads the authorization
        // `code` out of the authorize 302 Location itself.
        services.AddScoped<IParticipantSessionHandoff, ParticipantSessionHandoff>();

        // Password account paths (M2e): Keycloak Admin client (register) + ROPC/refresh/logout client (login).
        services.AddScoped<IMarineMobileKeycloakAdminClient, MarineMobileKeycloakAdminClient>();
        services.AddScoped<IParticipantKeycloakAuthClient, ParticipantKeycloakAuthClient>();

        // Central outgoing auth handler wired into every downstream Refit client.
        services.AddTransient<MarineMobileBffAuthDelegatingHandler>();

        // Foundation remote-call plumbing proof: Identity + ReferenceData only.
        services.AddTransient<IIdentityRemoteCall>(provider =>
            CreateRemoteCall<IIdentityRemoteCall>(
                CreateHttpClient(provider, nameof(IIdentityRemoteCall))));

        services.AddTransient<IReferenceDataRemoteCall>(provider =>
            CreateRemoteCall<IReferenceDataRemoteCall>(
                CreateHttpClient(provider, nameof(IReferenceDataRemoteCall))));

        services.AddTransient<IFileStorageRemoteCall>(provider =>
            CreateRemoteCall<IFileStorageRemoteCall>(
                CreateHttpClient(provider, nameof(IFileStorageRemoteCall))));

        // Vessel module (M4a reads + M4b create trio). Base URL from RemoteCalls:IVesselRemoteCall:BaseUrl.
        services.AddTransient<IVesselRemoteCall>(provider =>
            CreateRemoteCall<IVesselRemoteCall>(
                CreateHttpClient(provider, nameof(IVesselRemoteCall))));

        // ServiceRequest module owner endpoints (BE_MO1). Base URL from RemoteCalls:IServiceRequestRemoteCall:BaseUrl.
        services.AddTransient<IServiceRequestRemoteCall>(provider =>
            CreateRemoteCall<IServiceRequestRemoteCall>(
                CreateHttpClient(provider, nameof(IServiceRequestRemoteCall))));

        // Payment module owner participant-membership endpoints (BE_MO7). Base URL from
        // RemoteCalls:IParticipantMembershipRemoteCall:BaseUrl (→ the payment-api host).
        services.AddTransient<IParticipantMembershipRemoteCall>(provider =>
            CreateRemoteCall<IParticipantMembershipRemoteCall>(
                CreateHttpClient(provider, nameof(IParticipantMembershipRemoteCall))));

        // Notification module owner endpoints (BE_MO9c: inbox / read / device-token / preferences). Base URL from
        // RemoteCalls:INotificationRemoteCall:BaseUrl (→ the notification-api host).
        services.AddTransient<INotificationRemoteCall>(provider =>
            CreateRemoteCall<INotificationRemoteCall>(
                CreateHttpClient(provider, nameof(INotificationRemoteCall))));

        return services;
    }

    private static HttpClient CreateHttpClient(IServiceProvider provider, string clientName)
    {
        var authHandler = provider.GetRequiredService<MarineMobileBffAuthDelegatingHandler>();
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
