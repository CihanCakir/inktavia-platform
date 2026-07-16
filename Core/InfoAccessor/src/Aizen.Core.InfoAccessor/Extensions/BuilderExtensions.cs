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
        // Disabled unless BffAssertion:SharedSecret is set.
        //
        // When it IS set, the module trusts whatever user id the caller asserts. The only thing distinguishing
        // a trusted BFF from any other holder of a valid service-account token is AllowedClientIds. Leaving
        // that list empty turns the assertion into "any service client may claim to be any user" — and it
        // fails silently, because nothing about a wide-open allowlist looks wrong at runtime.
        //
        // So: secret set + allowlist empty ⇒ the service does not start. A misconfiguration that would grant
        // impersonation must be loud at boot, not discovered later.
        services.AddOptions<AizenBffAssertionOptions>()
            .Bind(configuration.GetSection(AizenBffAssertionOptions.SectionName))
            .Validate(
                o => string.IsNullOrWhiteSpace(o.SharedSecret) || o.AllowedClientIds is { Length: > 0 },
                "BffAssertion:SharedSecret is configured but BffAssertion:AllowedClientIds is empty. " +
                "An empty allowlist would let ANY caller holding a valid service-account token assert ANY " +
                "user id (X-Aizen-User-Id) and impersonate them. Configure the allowed client ids " +
                "(e.g. BffAssertion__AllowedClientIds__0=provider-portal-bff) or unset the shared secret.")
            .ValidateOnStart();

        services.AddSingleton<AizenInfoContainerForSigleton>();
        services.AddScoped<AizenInfoContainerForScoped>();
        services.AddScoped<IAizenInfoContainer, AizenInfoContainer>();
        services.AddScoped<IAizenInfoAccessor, AizenInfoAccessor>();

        // Forward all sub-accessor interfaces to the same scoped AizenInfoAccessor instance
        // so that consumers can inject IAizenUserInfoAccessor directly without going through
        // IAizenInfoAccessor.UserInfoAccessor.
        services.AddScoped<IAizenUserInfoAccessor>(sp =>
            sp.GetRequiredService<IAizenInfoAccessor>().UserInfoAccessor);

        // Injects Identity token roles (from X-Aizen-User-Token / AizenUserInfo) into the
        // ClaimsPrincipal after Keycloak service token authentication completes so that
        // [Authorize(Roles = ...)] can see application-level roles on internal module APIs.
        services.AddTransient<IClaimsTransformation, AizenIdentityClaimsTransformation>();

        return services;
    }
}