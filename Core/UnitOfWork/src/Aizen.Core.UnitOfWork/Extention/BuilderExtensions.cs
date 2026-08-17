using Aizen.Core.Common.Abstraction.Settings;
using Aizen.Core.UnitOfWork;
using Aizen.Core.UnitOfWork.Abstraction;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MiniUow.DependencyInjection;

namespace Aizen.Core.Infrastructure.UnitOfWork.Extension;

public class AizenUnitOfWorkOptions
{
    public bool UseMigration { get; set; } = false;
    public string MigrationAssembly { get; set; } = "";
    /// <summary>
    /// Enables EF Core lazy loading proxies. Requires all entity types to be non-sealed
    /// with virtual navigation properties. Set to false when entities are sealed.
    /// </summary>
    public bool UseLazyLoadingProxies { get; set; } = true;
}

public static class BuilderExtensions
{
    public static IServiceCollection AddAizenUnitOfWork<TContext>(this IServiceCollection services,
        IConfiguration configuration,
        string dbName,
        Action<AizenUnitOfWorkOptions> action = null)
        where TContext : DbContext
    {
        var options = new AizenUnitOfWorkOptions();
        action?.Invoke(options);
        var db = configuration.GetSection("DatabaseSettings").Get<DatabaseSettings>().First(x => x.Key == dbName);
        if (db.Value == null)
  throw new InvalidOperationException(
        $"DatabaseSettings altında '{dbName}' bulunamadı. " +
        $"appsettings.* içinde DatabaseSettings -> {{ \"{dbName}\": {{ ... }} }} ekleyin.");
        
        services.AddDbContext<TContext>(o =>
            {
                if (options.UseLazyLoadingProxies)
                    o.UseLazyLoadingProxies();
                switch (db.Value.Type)
                {
                    case DatabaseType.SqlLite:
                    case DatabaseType.MsSQL:
                        o.UseSqlServer(
                                db.Value.ConnectionString,
                                builder =>
                                {
                                    builder.CommandTimeout(60);
                                    if (options.UseMigration)
                                        builder.MigrationsAssembly(options.MigrationAssembly);
                                })
                            .ConfigureWarnings(warning => warning.Ignore(CoreEventId.DetachedLazyLoadingWarning));
                        break;
                    case DatabaseType.PostgreSQL:
                        o.UseNpgsql(
                                db.Value.ConnectionString,
                                builder =>
                                {
                                    builder.CommandTimeout(60);
                                    if (options.UseMigration)
                                        builder.MigrationsAssembly(options.MigrationAssembly);
                                })
                            .ConfigureWarnings(warning => warning.Ignore(CoreEventId.DetachedLazyLoadingWarning));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        ).AddUnitOfWork<TContext>();
        services.AddScoped<IAizenUnitOfWork, AizenUnitOfWork<TContext>>();
        services.AddScoped<IAizenUnitOfWork<TContext>, AizenUnitOfWork<TContext>>();
        services.Scan(scanner =>
        {
            // Exclude the non-generic IAizenUnitOfWork from the scan: it is already registered explicitly above,
            // and the command decorator injects IEnumerable<IAizenUnitOfWork> and calls SaveChangesAsync() once
            // PER registration. A second (duplicate) registration here made every command save TWICE per request
            // — a re-INSERT of the still-tracked new entity (duplicate rows / doubled side-effects; surfaced as a
            // unique-index violation on vessel create). The UoW stays the sole save, firing exactly once.
            scanner.AddTypes(typeof(AizenUnitOfWork<>))
                .AsImplementedInterfaces(t => t != typeof(IAizenUnitOfWork))
                .WithScopedLifetime();
        });

        return services;
    }
}