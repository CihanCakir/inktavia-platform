using LinqKit;
using MassTransit;
using Aizen.Core.Common.Abstraction.Helpers;
using Aizen.Core.Domain;
using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Core.Messagebus.Abstraction.Settings;
using Aizen.Core.Messagebus.Consumers;
using Aizen.Core.Messagebus.Middleware;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aizen.Core.Messagebus.Extentions;

public static class BuilderExtensions
{
    public static IServiceCollection AddAizenMessagebus(this IServiceCollection services,
        IConfiguration configuration, Action<EventConfigurationSettings> setupAction = null)
    {
        var options = new EventConfigurationSettings();
        setupAction?.Invoke(options);

        var messageBrokerQueueSettings =
            configuration.GetSection("MessageBroker:QueueSettings").Get<MessageBrokerQueueSettings>();

        services.AddMassTransit(x =>
        {
            x.SetEndpointNameFormatter(new CustomEndpointNameFormatter(messageBrokerQueueSettings));

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.MessageTopology.SetEntityNameFormatter(
                    new CustomMessageNameFormatter(cfg.MessageTopology.EntityNameFormatter,
                        messageBrokerQueueSettings));
                cfg.Host(messageBrokerQueueSettings.HostName, messageBrokerQueueSettings.VirtualHost, h =>
                {
                    h.Username(messageBrokerQueueSettings.UserName);
                    h.Password(messageBrokerQueueSettings.Password);
                });

                cfg.UseConsumeFilter(typeof(ElasticApmMiddleware<>), context);

                cfg.ConfigureEndpoints(context);
            });

            var consumers = AizenModuleAssemblyDiscovery.GetInstance().ModuleAssemblies.SelectMany(x => x.GetTypes())
                .Where(x => x is { IsClass: true, IsAbstract: false } &&
                            typeof(IAizenMessageConsumer).IsAssignableFrom(x) && !x.IsGenericType);
            foreach (var consumer in consumers)
            {
                if (options.AddConsumer)
                {
                    x.AddConsumer(consumer);
                }

                if (options.AddRequestClient)
                {
                    x.AddRequestClient(consumer);
                }

                services.AddScoped(typeof(IAizenMessageConsumer), consumer);
            }

            AizenModuleAssemblyDiscovery.GetInstance().DomainAssemblies.ForEach(assembly =>
            {
                assembly.GetTypes()
                    .Where(type =>
                        type is { IsClass: true, IsAbstract: false } && typeof(AizenEntity).IsAssignableFrom(type))
                    .ForEach(type =>
                    {
                        Type genericTypeDefinition = typeof(AizenGenericConsumer<>);
                        Type[] typeArguments = { type };
                        Type constructedType = genericTypeDefinition.MakeGenericType(typeArguments);

                        if (options.AddConsumer)
                        {
                            x.AddConsumer(constructedType);
                        }

                        if (options.AddRequestClient)
                        {
                            x.AddRequestClient(constructedType);
                        }

                        services.AddScoped(typeof(IAizenMessageConsumer), constructedType);
                    });
            });
        });

        services.AddScoped<IAizenMessagePublisher, AizenMessagePublisher>();

        return services;
    }
}