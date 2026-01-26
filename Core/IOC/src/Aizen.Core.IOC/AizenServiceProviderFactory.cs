using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Aizen.Core.IOC;

public class AizenServiceProviderFactory : IServiceProviderFactory<ContainerBuilder>
{
    public ContainerBuilder CreateBuilder(IServiceCollection services)
    {
        var builder = new ContainerBuilder();
        builder.Populate(services);
        builder.RegisterType<AizenServiceProvider>()
            .As<IServiceProvider>()
            .As<ISupportRequiredService>()
            .As<IServiceProviderIsService>()
            .ExternallyOwned();

        builder.RegisterType<AizenServiceScopeFactory>()
            .As<IServiceScopeFactory>()
            .SingleInstance();

        builder.ComponentRegistryBuilder.Registered += (sender, args) =>
        {
            args.ComponentRegistration.PipelineBuilding += (sender2, pipeline) =>
            {
                pipeline.Use(new AizenServiceMiddleware());
            };
        };

        return builder;
    }

    public IServiceProvider CreateServiceProvider(ContainerBuilder containerBuilder)
    {
        var autofacServiceProviderFactory = new AutofacServiceProviderFactory();
        var autofacServiceProvider =
            (AutofacServiceProvider) autofacServiceProviderFactory.CreateServiceProvider(containerBuilder);

        return new AizenServiceProvider(autofacServiceProvider);
    }
}