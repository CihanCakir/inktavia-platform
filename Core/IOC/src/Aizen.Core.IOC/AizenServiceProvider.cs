using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Aizen.Core.IOC;

public class AizenServiceProvider : IServiceProvider, ISupportRequiredService, IServiceProviderIsService,
    IDisposable, IAsyncDisposable
{
    private readonly AutofacServiceProvider _autofacServiceProvider;
    public AizenServiceProvider(AutofacServiceProvider autofacServiceProvider)
    {
        _autofacServiceProvider = autofacServiceProvider;
    }
    
    public AizenServiceProvider(ILifetimeScope lifetimeScope)
    {
        _autofacServiceProvider = new AutofacServiceProvider(lifetimeScope.BeginLifetimeScope());
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

    public void Dispose()
    {
        _autofacServiceProvider.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        return _autofacServiceProvider.DisposeAsync();
    }
}