using Microsoft.Extensions.DependencyInjection;
using Aizen.Core.Realtime.Abstraction.Domain;

namespace Aizen.Core.Realtime.Extensions;
public static class DomainRegistrationExtensions
{
    public static IServiceCollection AddRealtimeDomainEvents(this IServiceCollection services, string domain, params string[] eventNames)
    {
        RealtimeEventRegistry.RegisterDomainEvents(domain, eventNames);
        return services;
    }

    public static IServiceCollection AddRealtimeDomainRegistrar<TRegistrar>(this IServiceCollection services)
        where TRegistrar : class, Aizen.Core.Realtime.Abstraction.Interfaces.IRealtimeDomainRegistrar
    {
        services.AddSingleton<Aizen.Core.Realtime.Abstraction.Interfaces.IRealtimeDomainRegistrar, TRegistrar>();
        return services;
    }
}