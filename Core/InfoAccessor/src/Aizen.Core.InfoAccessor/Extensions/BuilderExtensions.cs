using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.InfoAccessor.Settings;
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

        return services;
    }
}