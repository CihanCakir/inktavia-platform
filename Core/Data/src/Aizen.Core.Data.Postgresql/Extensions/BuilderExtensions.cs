using Aizen.Core.Data.Abstraction;
using Aizen.Core.Data.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Aizen.Core.Common.Abstraction.Settings;

namespace Aizen.Core.Data.Postgresql.Extensions;
public static class BuilderExtensions
{
    public static IServiceCollection AddAizenPostgresqlDatabase(this IServiceCollection services,
        IConfiguration configuration,
        Action<DatabaseSetting>? setupAction = default)
    {
        services.AddAizenDatabase(configuration);
        if (setupAction != null)
        {
            services.Configure(setupAction);
        }

        services.AddScoped<IAizenDatabaseConnectionFactory, AizenDatabasePostgresqlConnectionFactory>();
        
        return services;
    }
}