using Aizen.Core.Common.Abstraction.Exception;
using Aizen.Core.Common.Abstraction.Helpers;
using Aizen.Core.Common.Abstraction.Settings;
using Aizen.Core.IOC.Abstraction.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Aizen.Core.Configuration.Extentions;
namespace Aizen.Core.Data.Mongo.Extensions
{
    public class AizenMongoOptions
    {
    }

    public static class BuilderExtensions
    {
        public static IServiceCollection AddAizenMongo(this IServiceCollection services,
            IConfiguration configuration, Action<AizenMongoOptions> setupAction = null)
        {
            if (services == null)
            {
                throw new AizenException($"ServiceCollection: {nameof(services)} not found.");
            }

            services.AddScoped(typeof(IAizenMongoRepositoryFactory<>), typeof(AizenMongoRepositoryFactory<>));

            var moduleDiscovery = AizenModuleAssemblyDiscovery.GetInstance();
            services.ToScan(scan => scan.FromAssemblies(moduleDiscovery.RepositoryAssemblies)
                .AddClasses(x => x.AssignableTo(typeof(AizenMongoContext)))
                .As<AizenMongoContext>()
                .AsSelf()
                .AsImplementedInterfaces()
                .WithScopedLifetime());

            foreach (var assembly in moduleDiscovery.RepositoryAssemblies)
            {
                var dbContexts = assembly.GetTypes()
                    .Where(x => x.IsSubclassOf(typeof(AizenMongoContext)))
                    .ToList();

                foreach (var dbContext in dbContexts)
                {
                    var serviceType = typeof(IAizenMongoRepositoryFactory<>).MakeGenericType(dbContext);
                    var implementationType = typeof(AizenMongoRepositoryFactory<>).MakeGenericType(dbContext);

                    services.AddScoped(serviceType, implementationType);
                }
            }

            if (setupAction != null)
            {
                services.Configure(setupAction);
            }
             services.ConfigureDictionary<DatabaseSettings, DatabaseSetting>(
              configuration.GetSection(nameof(DatabaseSettings)));

            return services;
        }
    }
}
