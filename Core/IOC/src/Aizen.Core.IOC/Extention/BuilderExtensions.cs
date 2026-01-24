using Aizen.Core.Common.Abstraction.Exception;
using Aizen.Core.Common.Abstraction.Helpers;
using Aizen.Core.IOC.Abstraction.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aizen.Core.IOC.Extention;

public static class BuilderExtensions
{
    public static AizenServiceProviderFactory UseAizenServiceProviderFactory(this IHostBuilder hostbuilder)
    {
        var serviceProviderFactory = new AizenServiceProviderFactory();
        hostbuilder.UseServiceProviderFactory(serviceProviderFactory);

        return serviceProviderFactory;
    }
    
    public static IServiceCollection AddAizenIOC(this IServiceCollection services, IConfiguration configuration)
    {
        if (services == null)
        {
            throw new AizenException($"ServiceCollection: {nameof(services)} not found.");
        }

        var moduleDiscovery = AizenModuleAssemblyDiscovery.GetInstance();
        services.Scan(scan => scan.FromAssemblies(moduleDiscovery.ApplicationAssemblies)
            .AddClasses(x => x.AssignableTo(typeof(IAizenServiceScope)))
            .AsImplementedInterfaces()
            .AsSelf()
            .WithScopedLifetime());

        services.Scan(scan => scan.FromAssemblies(moduleDiscovery.ApplicationAssemblies)
            .AddClasses(x => x.AssignableTo(typeof(IAizenServiceTransient)))
            .AsImplementedInterfaces()
            .AsSelf()
            .WithTransientLifetime());

        services.Scan(scan => scan.FromAssemblies(moduleDiscovery.ApplicationAssemblies)
            .AddClasses(x => x.AssignableTo(typeof(IAizenServiceSingleton)))
            .AsImplementedInterfaces()
            .AsSelf()
            .WithSingletonLifetime());
        
        services.Scan(scan => scan.FromAssemblies(moduleDiscovery.RepositoryAssemblies)
            .AddClasses(x => x.AssignableTo(typeof(IAizenServiceScope)))
            .AsImplementedInterfaces()
            .AsSelf()
            .WithScopedLifetime());

        services.Scan(scan => scan.FromAssemblies(moduleDiscovery.RepositoryAssemblies)
            .AddClasses(x => x.AssignableTo(typeof(IAizenServiceTransient)))
            .AsImplementedInterfaces()
            .AsSelf()
            .WithTransientLifetime());

        services.Scan(scan => scan.FromAssemblies(moduleDiscovery.RepositoryAssemblies)
            .AddClasses(x => x.AssignableTo(typeof(IAizenServiceSingleton)))
            .AsImplementedInterfaces()
            .AsSelf()
            .WithSingletonLifetime());

        return services;
    }
}