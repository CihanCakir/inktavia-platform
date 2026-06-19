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

        services.AddSingleton<AizenInfoContainerForSigleton>();
        services.AddScoped<AizenInfoContainerForScoped>();
        services.AddScoped<IAizenInfoContainer, AizenInfoContainer>();
        services.AddScoped<IAizenInfoAccessor, AizenInfoAccessor>();

        // Injects Identity token roles (from X-Aizen-User-Token / AizenUserInfo) into the
        // ClaimsPrincipal after Keycloak service token authentication completes so that
        // [Authorize(Roles = ...)] can see application-level roles on internal module APIs.
        services.AddTransient<IClaimsTransformation, AizenIdentityClaimsTransformation>();

        return services;
    }
}