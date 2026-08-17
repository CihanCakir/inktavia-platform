using Aizen.Modules.Content.Application.Services;
using Aizen.Modules.Content.Domain.Interface.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aizen.Modules.Content.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the Content application services (domain services + cache seam).
    /// CQRS command/query handlers and FluentValidation validators are discovered automatically
    /// by the Aizen CQRS pipeline via assembly scanning (same house pattern as the sibling modules),
    /// so they are not registered explicitly here.
    /// </summary>
    public static IServiceCollection AddContentApplicationServices(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ISlugService, SlugService>();
        services.AddScoped<IContentCacheInvalidator, ContentCacheInvalidator>();
        services.AddScoped<IContentMediaValidator, ContentMediaValidator>();   // B3 — FileStorage
        services.AddSingleton<ILanguageValidator, LanguageValidator>();        // B4 — config-backed set

        _ = configuration;
        return services;
    }
}
