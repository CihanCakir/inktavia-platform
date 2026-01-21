using EventStore.Client;
using Aizen.Core.EventStore.Abstraction;
using Aizen.Core.EventStore.Abstraction.Projection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aizen.Core.EventStore.Extension;

public static class BuilderExtensions
{
    public static IServiceCollection AddAizenEventStore(this IServiceCollection services,
        IConfiguration configuration)
    {
        var eventStoreConfigurationSection = configuration.GetSection("EventStore");
        services.Configure<EventStoreConfiguration>(eventStoreConfigurationSection);

        var eventStoreConfiguration = eventStoreConfigurationSection.Get<EventStoreConfiguration>();

        if (!string.IsNullOrEmpty(eventStoreConfiguration.ConnectionString))
        {
            var clientSettings = EventStoreClientSettings.Create(eventStoreConfiguration.ConnectionString);
            var eventStoreClient = new EventStoreClient(clientSettings);
            var projectionManagementClient = new EventStoreProjectionManagementClient(clientSettings);

            services.AddSingleton(eventStoreClient);
            services.AddSingleton(projectionManagementClient);
            
            services.AddScoped<IEventStorePublisher, EventStorePublisher>();
            services.AddScoped<IEventStoreProjectionManager, EventStorePublisher>();
        }

        return services;
    }
}