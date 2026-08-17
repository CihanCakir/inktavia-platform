using Aizen.Bff.Marine.Web.Application.Common.Http;
using Aizen.Bff.Marine.Web.Application.Common.Options;
using Aizen.Bff.Marine.Web.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Web.Application.Common.Seo;
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

        // W2 — public-surface options (trusted server-caller secret + revalidation webhook). Secrets are
        // environment/secret-store only; no data-annotation Required (an empty secret disables the feature).
        services.AddOptions<MarineWebPublicOptions>()
            .Bind(configuration.GetSection(MarineWebPublicOptions.SectionName));

        // W2.2 — best-effort outbound cache-revalidation webhook to the Next.js server (fired by the bus consumers).
        services.AddScoped<IWebRevalidationNotifier, WebRevalidationNotifier>();

        // W3.2 — SEO indexability seam. Options-backed today (MarineWebPublic:Seo); swappable for managed data later
        // without touching the handlers (they depend on ISeoIndexabilityPolicy). Stateless → singleton.
        services.AddSingleton<ISeoIndexabilityPolicy, OptionsSeoIndexabilityPolicy>();

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

        // W4/M1 — Payment module client (public subscription plans + published pricing terms for the pricing page).
        // Base URL from RemoteCalls:IPaymentRemoteCall:BaseUrl (→ the payment-api host). All reads are anonymous on
        // the module, so no additional service role is required.
        services.AddTransient<IPaymentRemoteCall>(provider =>
            CreateRemoteCall<IPaymentRemoteCall>(
                CreateHttpClient(provider, nameof(IPaymentRemoteCall))));

        // M4 — Notification module client (public contact intake). Base URL from
        // RemoteCalls:INotificationRemoteCall:BaseUrl (→ the notification-api host). The endpoint is anonymous on the
        // module, so no additional service role is required.
        services.AddTransient<INotificationRemoteCall>(provider =>
            CreateRemoteCall<INotificationRemoteCall>(
                CreateHttpClient(provider, nameof(INotificationRemoteCall))));

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
