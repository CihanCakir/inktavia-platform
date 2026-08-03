using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Aizen.Core.IOC;

public class AizenServiceProvider : IServiceProvider, ISupportRequiredService, IServiceProviderIsService,
    IKeyedServiceProvider, IDisposable, IAsyncDisposable
{
    private readonly AutofacServiceProvider _autofacServiceProvider;
    public AizenServiceProvider(AutofacServiceProvider autofacServiceProvider)
    {
        _autofacServiceProvider = autofacServiceProvider;
    }
    
    public AizenServiceProvider(ILifetimeScope lifetimeScope)
    {
        _autofacServiceProvider = new AutofacServiceProvider(lifetimeScope);
    }

    public object? GetService(Type serviceType)
    {
        var result = _autofacServiceProvider.GetService(serviceType);
        return result;
    }

    public object GetRequiredService(Type serviceType)
    {
        var result = _autofacServiceProvider.GetRequiredService(serviceType);
        return result;
    }

    public bool IsService(Type serviceType)
    {
        return _autofacServiceProvider.IsService(serviceType);
    }

    public object? GetKeyedService(Type serviceType, object? serviceKey)
    {
        return KeyedProvider.GetKeyedService(serviceType, serviceKey);
    }

    public object GetRequiredKeyedService(Type serviceType, object? serviceKey)
    {
        return KeyedProvider.GetRequiredKeyedService(serviceType, serviceKey);
    }

    private IKeyedServiceProvider KeyedProvider =>
        _autofacServiceProvider as IKeyedServiceProvider
        ?? throw new InvalidOperationException(
            "The underlying AutofacServiceProvider does not support keyed services. " +
            "Ensure Autofac.Extensions.DependencyInjection 9.0.0+ is referenced.");

    public void Dispose()
    {
        _autofacServiceProvider.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        return _autofacServiceProvider.DisposeAsync();
    }
}