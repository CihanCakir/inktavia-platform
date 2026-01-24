using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Aizen.Core.IOC;

public class AizenServiceScope : IServiceScope, IAsyncDisposable
{
    private readonly AutofacServiceProvider _serviceProvider;

    public AizenServiceScope(ILifetimeScope lifetimeScope)
    {
        _serviceProvider = new AutofacServiceProvider(lifetimeScope);
        ServiceProvider = new AizenServiceProvider(_serviceProvider);
    }

    public IServiceProvider ServiceProvider { get; }

    public void Dispose()
    {
        _serviceProvider.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        return _serviceProvider.DisposeAsync();
    }
}