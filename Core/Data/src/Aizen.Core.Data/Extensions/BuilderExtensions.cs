using Aizen.Core.Data.Abstraction;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Aizen.Core.Common.Abstraction.Settings;

namespace Aizen.Core.Data.Extensions;

public static class BuilderExtensions
{
    public static IServiceCollection AddAizenDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var seyirDatabaseOptions = configuration.GetSection("DatabaseSettings");
        services.Configure<DatabaseSettings>(seyirDatabaseOptions);
        foreach (var seyirDatabaseOption in seyirDatabaseOptions.GetChildren())
        {
            services.Configure<DatabaseSetting>(seyirDatabaseOption);
        }

        services.AddScoped<IAizenDatabaseConnectionManager, AizenDatabaseConnectionManager>();

        return services;
    }
}