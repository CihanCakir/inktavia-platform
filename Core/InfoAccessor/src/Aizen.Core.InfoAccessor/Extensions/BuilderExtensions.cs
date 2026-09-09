using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.InfoAccessor.Settings;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aizen.Core.InfoAccessor.Extensions;

public static class BuilderExtensions
{
    public static IServiceCollection AddAizenInfoAccessor(this IServiceCollection services,
        IConfiguration configuration, Action<InfoAccessorConfigurationSettings> setupAction = null)
    {
        var options = new InfoAccessorConfigurationSettings();
        setupAction?.Invoke(options);

        // ── Trusted-BFF identity assertion (module side) ─────────────────────────────────────────────────
        // The module trusts whatever user id a trusted BFF asserts. The only thing distinguishing a trusted
        // BFF from any other holder of a valid service-account token is AllowedClientIds. Leaving that list
        // empty turns the assertion into "any service client may claim to be any user" — and it fails
        // silently, because nothing about a wide-open allowlist looks wrong at runtime.
        //
        // Two fail-closed startup gates (HARDENING_GENERIC_CRUD_AND_SERVICE_TOKEN):
        //   (a) secret set + allowlist empty ⇒ the service does not start — a config that would grant
        //       impersonation must be loud at boot, not discovered later; and
        //   (b) OUTSIDE Development the secret must be non-empty — the internal service-token identity path
        //       must be actively configured in prod/staging, never left inert. In Development an empty secret
        //       still means "feature disabled" so local runs are unchanged.
        //
        // Environment is read from configuration (ASPNETCORE_ENVIRONMENT / DOTNET_ENVIRONMENT — both surfaced
        // into IConfiguration by the host's environment-variable source) with an env-var fallback, defaulting
        // to Production when unset (fail-closed default). No dependency on IHostEnvironment, so this stays a
        // single-argument extension usable by every host + hermetically testable.
        var environmentName =
            configuration["ASPNETCORE_ENVIRONMENT"]
            ?? configuration["DOTNET_ENVIRONMENT"]
            ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? "Production";
        var isDevelopment = string.Equals(environmentName, "Development", StringComparison.OrdinalIgnoreCase);

        services.AddOptions<AizenBffAssertionOptions>()
            .Bind(configuration.GetSection(AizenBffAssertionOptions.SectionName))
            .Validate(
                o => string.IsNullOrWhiteSpace(o.SharedSecret) || o.AllowedClientIds is { Length: > 0 },
                "BffAssertion:SharedSecret is configured but BffAssertion:AllowedClientIds is empty. " +
                "An empty allowlist would let ANY caller holding a valid service-account token assert ANY " +
                "user id (X-Aizen-User-Id) and impersonate them. Configure the allowed client ids " +
                "(e.g. BffAssertion__AllowedClientIds__0=provider-portal-bff) or unset the shared secret.")
            .Validate(
                o => isDevelopment || !string.IsNullOrWhiteSpace(o.SharedSecret),
                $"BffAssertion:SharedSecret is required outside Development (environment='{environmentName}') " +
                "but is empty. The internal BFF→module service-token identity path must be configured in " +
                "non-Development environments (fail-closed). Wire BffAssertion__SharedSecret from your env/k8s " +
                "secret (e.g. AIZEN_BFF_ASSERTION_SECRET), or run with ASPNETCORE_ENVIRONMENT=Development locally.")
            .ValidateOnStart();

        // #111: host probes (cpuinfo/meminfo/…) run once per process, not once per scoped container.
        services.AddSingleton<AizenServerInfoProvider>();
        services.AddSingleton<AizenInfoContainerForSigleton>();
        services.AddScoped<AizenInfoContainerForScoped>();
        services.AddScoped<IAizenInfoContainer, AizenInfoContainer>();
        services.AddScoped<IAizenInfoAccessor, AizenInfoAccessor>();

        // Forward all sub-accessor interfaces to the same scoped AizenInfoAccessor instance
        // so that consumers can inject IAizenUserInfoAccessor directly without going through
        // IAizenInfoAccessor.UserInfoAccessor.
        services.AddScoped<IAizenUserInfoAccessor>(sp =>
            sp.GetRequiredService<IAizenInfoAccessor>().UserInfoAccessor);

        // Faz 28.1: IAizenClientInfoAccessor'ı doğrudan enjekte eden İLK tüketici RecipientLocaleResolver.
        // Bu forward olmadan RecipientLocaleResolver -> SendNotificationCommandHandler aktive edilemiyordu
        // (Autofac activation hatası → tüm bildirim gönderimleri, OTP e-postaları dahil bloke).
        // NOT: message-consumer kapsamlarında AizenInfoContainer.Get default döner → ClientInfo null olabilir;
        // bu yüzden çağıranlar null-toleranslı olmalı (RecipientLocaleResolver zaten öyle).
        // İLERİDE TEMİZLİK: diğer alt-erişimci arayüzleri (Device/Network/Request/Server/Channel/Execution/Keycloak
        // token) da benzer forward'larla eklenebilir; şu an yalnız fiilen ihtiyaç duyulan ClientInfo forward ediliyor.
        services.AddScoped<IAizenClientInfoAccessor>(sp =>
            sp.GetRequiredService<IAizenInfoAccessor>().ClientInfoAccessor);

        // Injects Identity token roles (from X-Aizen-User-Token / AizenUserInfo) into the
        // ClaimsPrincipal after Keycloak service token authentication completes so that
        // [Authorize(Roles = ...)] can see application-level roles on internal module APIs.
        services.AddTransient<IClaimsTransformation, AizenIdentityClaimsTransformation>();

        return services;
    }
}