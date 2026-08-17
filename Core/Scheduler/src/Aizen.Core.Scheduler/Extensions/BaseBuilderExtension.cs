using Elastic.Apm.MongoDb;
using Hangfire;
using Hangfire.Console;
using Hangfire.Heartbeat;
using Hangfire.MemoryStorage;
using Hangfire.Mongo;
using Hangfire.PostgreSql;
using Aizen.Core.Common.Abstraction.Enums;
using Aizen.Core.Common.Abstraction.Exception;
using Aizen.Core.Common.Abstraction.Settings;
using Aizen.Core.Configuration;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Scheduler.Abstraction;
using Aizen.Core.Scheduler.JobFilters;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Driver;

namespace Aizen.Core.Scheduler.Extensions
{
    public class AizenSchedulerOptions
    {
        public AizenSchedulerStorageType StorageType { get; set; } = AizenSchedulerStorageType.MsSQL;
        public int RetryAttempts { get; set; }
        public bool UseFilter { get; set; } = true;
        public string RouteUrl { get; set; }
        public string DatabaseKey { get; set; } = "Scheduler";
        /// <summary>
        /// PostgreSQL schema name for Hangfire tables. Defaults to "hangfire".
        /// Set per-module to isolate job tables (e.g. "cargodry-scheduler").
        /// </summary>
        public string SchemaName { get; set; } = "hangfire";
    }

    internal static class BaseBuilderExtension
    {
        private static bool _isServiceAdded;
        private static bool _isMiddlewareAdded;

        private static readonly int _heartbeatInterval = 3;

        public static IServiceCollection AddAizenBaseScheduler(this IServiceCollection services,
            Action<AizenSchedulerOptions> setupAction = null)
        {
            if (services == null)
            {
                throw new AizenException($"ServiceCollection: {nameof(services)} not found.");
            }

            if (_isServiceAdded)
            {
                return services;
            }

            _isServiceAdded = true;

            AizenSchedulerOptions options =
                AizenConfiguration.Configuration.GetSection("Scheduler").Get<AizenSchedulerOptions>() ??
                new AizenSchedulerOptions();
            if (setupAction != null)
            {
                setupAction.Invoke(options);
            }

            services.AddSingleton(options);
            services.AddScoped<IAizenSchedulerLogger, AizenSchedulerLogger>();

            switch (options.StorageType)
            {
                case AizenSchedulerStorageType.InMemory:
                    services.AddHangfire((provider, config) =>
                    {
                        config.UseMemoryStorage();
                        config.UseConsole();
                        if (options.UseFilter)
                        {
                            config.UseFilter(new AizenJobFilterAttribute(provider));
                        }
                        else
                        {
                            config.UseFilter(new HttpContextJobFilterAttribute(provider));
                        }

                        config.UseHeartbeatPage(checkInterval: TimeSpan.FromSeconds(_heartbeatInterval));
                    });
                    break;
                case AizenSchedulerStorageType.PostgreSQL:
                {
                    var databaseSettings = AizenConfiguration.Configuration.GetSection("DatabaseSettings")
                        .Get<DatabaseSettings>()
                        .First(x => x.Key == options.DatabaseKey);

                    services.AddHangfire((provider, config) =>
                    {
                        config.UseConsole();
                        if (options.UseFilter)
                        {
                            config.UseFilter(new AizenJobFilterAttribute(provider));
                        }
                        else
                        {
                            config.UseFilter(new HttpContextJobFilterAttribute(provider));
                        }

                        config.UsePostgreSqlStorage(o =>
                        {
                            o.UseNpgsqlConnection(databaseSettings.Value.ConnectionString);
                        }, new PostgreSqlStorageOptions
                        {
                            SchemaName = options.SchemaName,
                        });
                        config.UseHeartbeatPage(checkInterval: TimeSpan.FromSeconds(_heartbeatInterval));
                    });
                }
                    break;
                case AizenSchedulerStorageType.Mongo:
                {
                    var databaseSettings = AizenConfiguration.Configuration.GetSection("DatabaseSettings")
                        .Get<DatabaseSettings>()
                        .First(x => x.Key == options.DatabaseKey);

                    var mongoConnectionString = databaseSettings.Value.ConnectionString;
                    var mongoDatabaseName = new MongoUrl(mongoConnectionString).DatabaseName;

                    services.AddHangfire((provider, config) =>
                    {
                        //TODO: Test için göndeiliyor. Duruma göre refactor atılacak.
                        var mongoConnectionUrl = new MongoUrl(mongoConnectionString);
                        var mongoClientSettings = MongoClientSettings.FromUrl(mongoConnectionUrl);
                        mongoClientSettings.ClusterConfigurator = builder => builder.Subscribe(new MongoDbEventSubscriber());

                        var mongoClient = new MongoClient(mongoClientSettings);

                        config.UseMongoStorage(mongoClient, mongoDatabaseName, new MongoStorageOptions
                        {
                            ByPassMigration = true,
                            CountersAggregateInterval = TimeSpan.FromMinutes(5),
                            JobExpirationCheckInterval = TimeSpan.FromHours(1),
                        });
                        config.UseConsole(new ConsoleOptions());
                        if (options.UseFilter)
                        {
                            config.UseFilter(new AizenJobFilterAttribute(provider));
                        }
                        else
                        {
                            config.UseFilter(new HttpContextJobFilterAttribute(provider));
                        }

                        config.UseHeartbeatPage(checkInterval: TimeSpan.FromSeconds(_heartbeatInterval));
                    });
                }
                    break;
                default:
                    services.AddHangfire((provider, config) =>
                    {
                        config.UseMemoryStorage();
                        config.UseConsole();
                        if (options.UseFilter)
                        {
                            config.UseFilter(new AizenJobFilterAttribute(provider));
                        }
                        else
                        {
                            config.UseFilter(new HttpContextJobFilterAttribute(provider));
                        }

                        config.UseHeartbeatPage(checkInterval: TimeSpan.FromSeconds(_heartbeatInterval));
                    });
                    break;
            }


            //TODO: Method based attribute kullanımı imp. edilene kadar devam.
            if (options.RetryAttempts != AutomaticRetryAttribute.DefaultRetryAttempts)
            {
                GlobalJobFilters.Filters.Add(new AutomaticRetryAttribute {Attempts = options.RetryAttempts});
            }

            services.AddHangfireServer((provider, serverOptions) =>
            {
                using (var scope = provider.CreateScope())
                {
                    var infoAccessor = scope.ServiceProvider.GetRequiredService<IAizenInfoAccessor>();
                    var queueName =
                        $"{infoAccessor.AppInfoAccessor.AppInfo.Name}-{infoAccessor.AppInfoAccessor.AppInfo.Type}"
                            .ToLower();
                    serverOptions.Queues = new[]
                    {
                        queueName
                    };
                }
            });

            services.Replace(
                new ServiceDescriptor(typeof(JobActivator),
                    serviceProvider => new AizenJobActivator(serviceProvider),
                    ServiceLifetime.Singleton));

            return services;
        }

        public static void UseBaseAizenScheduler(this IApplicationBuilder app)
        {
            if (_isMiddlewareAdded)
            {
                return;
            }

            _isMiddlewareAdded = true;
            var option = app.ApplicationServices.GetRequiredService<AizenSchedulerOptions>();

            app.UseHangfireDashboard(options: new DashboardOptions()
            {
                PrefixPath = string.IsNullOrEmpty(option.RouteUrl) ? "" : option.RouteUrl,
            });
        }
    }
}