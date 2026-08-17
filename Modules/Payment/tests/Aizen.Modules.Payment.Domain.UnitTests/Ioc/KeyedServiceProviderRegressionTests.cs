using Aizen.Core.IOC;
using Aizen.Modules.Payment.Abstraction.Interface;
using Aizen.Modules.Payment.Application.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Domain.UnitTests.Ioc;

/// <summary>
/// Regression guard for FIX_KEYED_SERVICE_DI. The Aizen decorator (<see cref="AizenServiceProvider"/>),
/// injected everywhere as <see cref="IServiceProvider"/>, must expose keyed services so
/// <see cref="PaymentGatewayResolver"/> can resolve the active gateway via
/// <c>GetKeyedService&lt;IPaymentGatewayProvider&gt;("manual"/"iyzico")</c>. Before the fix this threw
/// "This service provider doesn't support keyed services".
/// </summary>
public sealed class KeyedServiceProviderRegressionTests
{
    private static IServiceProvider BuildAizenContainer()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        // Same keyed registration used by Payment.Application.DependencyInjection.
        services.AddKeyedScoped<IPaymentGatewayProvider, ManualPaymentGatewayProvider>("manual");

        var factory = new AizenServiceProviderFactory();
        var builder = factory.CreateBuilder(services);
        return factory.CreateServiceProvider(builder);
    }

    [Fact]
    public void GetKeyedService_ResolvesManualGateway_ThroughAizenContainer()
    {
        var root = BuildAizenContainer();
        // Keyed services are scoped — resolve inside a request scope (the AizenServiceScope path).
        using var scope = root.CreateScope();
        var provider = scope.ServiceProvider;

        provider.Should().BeOfType<AizenServiceProvider>(
            "the scope's provider is the Aizen decorator that must support keyed services");

        var gateway = provider.GetKeyedService<IPaymentGatewayProvider>("manual");

        gateway.Should().NotBeNull();
        gateway.Should().BeOfType<ManualPaymentGatewayProvider>();
        gateway!.ProviderKey.Should().Be("manual");
    }

    [Fact]
    public void GetRequiredKeyedService_ResolvesManualGateway_ThroughAizenContainer()
    {
        var root = BuildAizenContainer();
        using var scope = root.CreateScope();

        var gateway = scope.ServiceProvider.GetRequiredKeyedService<IPaymentGatewayProvider>("manual");

        gateway.Should().BeOfType<ManualPaymentGatewayProvider>();
    }

    [Fact]
    public void AizenServiceProvider_Implements_IKeyedServiceProvider()
    {
        // The MS GetKeyedService extension casts the provider to IKeyedServiceProvider; the decorator must implement it.
        typeof(IKeyedServiceProvider).IsAssignableFrom(typeof(AizenServiceProvider)).Should().BeTrue();
    }
}
