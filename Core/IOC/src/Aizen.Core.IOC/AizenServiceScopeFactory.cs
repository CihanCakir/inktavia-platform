using Autofac;
using Microsoft.Extensions.DependencyInjection;

namespace Aizen.Core.IOC;

public class AizenServiceScopeFactory : IServiceScopeFactory
{
    private readonly ILifetimeScope _lifetimeScope;

    public AizenServiceScopeFactory(ILifetimeScope lifetimeScope)
    {
        _lifetimeScope = lifetimeScope;
    }
    
    public IServiceScope CreateScope()
    {
        return new AizenServiceScope(_lifetimeScope.BeginLifetimeScope());
    }
}