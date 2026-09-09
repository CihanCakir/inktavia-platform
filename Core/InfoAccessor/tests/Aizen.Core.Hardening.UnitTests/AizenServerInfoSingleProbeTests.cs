using System.Runtime.InteropServices;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.InfoAccessor.Extensions;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aizen.Core.Hardening.UnitTests;

/// <summary>
/// Debt #111 regression: <see cref="AizenServerInfo"/> probes the host (shelling out on Linux) and MUST be
/// computed exactly once per process, never rebuilt when a new scoped <c>AizenInfoContainer</c> is created
/// per HTTP request. The proof is reference identity: a single instance shared by every scope means the
/// (expensive) population ran once.
/// </summary>
public sealed class AizenServerInfoSingleProbeTests
{
    private static ServiceProvider BuildProvider()
    {
        // Development: BffAssertion secret requirement (fail-closed) is off → minimal setup suffices.
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Development",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(config);
        services.AddAizenInfoAccessor(config);

        return services.BuildServiceProvider(validateScopes: true);
    }

    private static AizenServerInfo ResolveServerInfo(IServiceProvider scopeProvider)
        => scopeProvider.GetRequiredService<IAizenInfoAccessor>().ServerInfoAccessor.ServerInfo;

    [Fact]
    public void ServerInfo_is_computed_once_across_multiple_scopes()
    {
        using var provider = BuildProvider();

        var instances = new List<AizenServerInfo>();
        for (var i = 0; i < 5; i++)
        {
            using var scope = provider.CreateScope();
            instances.Add(ResolveServerInfo(scope.ServiceProvider));
        }

        // Every scope returns the SAME instance ⇒ the host probes ran exactly once, not once per request.
        instances.Should().OnlyContain(info => ReferenceEquals(info, instances[0]));

        // MachineName must keep working — audit CreateHost/ModifyHost (AizenUnitOfWork) depend on it.
        instances[0].MachineName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ServerInfo_skips_gui_and_hardware_probes_on_linux()
    {
        // The probe-skip contract (#111) is Linux-only: lspci/xdpyinfo/dmidecode can never succeed in a
        // container, so they return "Unknown" without shelling out. On Windows/macOS the real probes run.
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return;

        using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var info = ResolveServerInfo(scope.ServiceProvider);

        info.GraphicsCard.Should().Be("Unknown");       // was: lspci
        info.DisplayResolution.Should().Be("Unknown");  // was: xdpyinfo
        info.Motherboard.Should().Be("Unknown");        // was: dmidecode
    }
}
