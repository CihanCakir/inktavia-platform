using System.Reflection;
using Aizen.Modules.CargoDry.Application;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Aizen.Modules.CargoDry.Application.UnitTests;

/// <summary>
/// Split-host DI guard. Builds the CargoDry.Application graph the way the standalone aizen-cargodry pod does —
/// <c>AddCargoDryApplication()</c> WITHOUT Payment.Application — and asserts EVERY MediatR handler in the assembly can be
/// activated. The reported bug was PrepareCargoDrySettlementPaymentCommandHandler injecting a Payment.Application-only
/// service, which 911'd on activation in this exact topology. This test would have caught it at build time and now guards
/// the whole module against that class: any handler wired to a service with no split-host registration fails here.
///
/// Only the CargoDry-owned concrete types (the handlers and the services AddCargoDryApplication registers) are actually
/// constructed; every external dependency (repositories, remote-call clients, info accessor, cache, ISender, …) is
/// auto-substituted, so a green run means "no handler is missing a registration", not "the collaborators behave".
/// </summary>
public sealed class CargoDryApplicationDiActivationTests
{
    [Fact]
    public void Every_handler_activates_without_Payment_Application()
    {
        var appAsm = typeof(DependencyInjection).Assembly;

        var services = new ServiceCollection();
        services.AddLogging();
        // Minimal host config so services that VALIDATE config in their constructor (e.g. ActivationTokenService) build.
        // This test is about DI registration presence (the 911 class), not about missing config values.
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CargoDry:ActivationTokenSecret"] = "unit-test-activation-token-secret-0001",
            })
            .Build();
        services.AddSingleton<IConfiguration>(configuration);

        // The split-host Application graph — NO Payment.Application registrations, exactly like aizen-cargodry.
        services.AddCargoDryApplication();

        var handlerTypes = appAsm.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && !t.ContainsGenericParameters)
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)))
            .ToList();

        handlerTypes.Should().NotBeEmpty("the CargoDry.Application assembly must expose MediatR command/query handlers");

        // The CargoDry-owned concrete impls that AddCargoDryApplication registered (services + bridge fallbacks/remote).
        var registeredOwnedImpls = services
            .Where(d => d.ImplementationType is not null && d.ImplementationType.Assembly == appAsm)
            .Select(d => d.ImplementationType!)
            .Distinct()
            .ToList();

        var known = new HashSet<Type>(services.Select(d => d.ServiceType));
        var queue = new Queue<Type>();

        foreach (var h in handlerTypes)
        {
            if (known.Add(h)) services.AddScoped(h); // resolve the concrete handler directly to exercise its ctor
            queue.Enqueue(h);
        }
        foreach (var impl in registeredOwnedImpls)
            queue.Enqueue(impl);

        // Fixed-point: construct CargoDry-owned concrete types for real, auto-substitute every external interface
        // dependency, and follow concrete deps transitively.
        var scanned = new HashSet<Type>();
        while (queue.Count > 0)
        {
            var t = queue.Dequeue();
            if (!scanned.Add(t)) continue;

            var ctor = t.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .OrderByDescending(c => c.GetParameters().Length)
                .FirstOrDefault();
            if (ctor is null) continue;

            foreach (var p in ctor.GetParameters())
            {
                var pt = p.ParameterType;

                if (pt.IsGenericType && pt.GetGenericTypeDefinition() == typeof(ILogger<>)) continue; // AddLogging
                if (pt == typeof(IConfiguration)) continue;
                if (known.Contains(pt))
                {
                    if (pt.IsClass && !pt.IsAbstract) queue.Enqueue(pt);
                    continue;
                }

                // CROSS-MODULE IN-PROCESS BRIDGE SEAM: an interface declared in ANOTHER module's *.Abstraction.Interface
                // (e.g. Payment's ICargoDrySettlementPayoutService) that is NOT a Starter-registered RemoteCall client.
                // These are the exact seams the split aizen-cargodry pod must self-register (real remote impl or a
                // clean-fail fallback). We deliberately do NOT auto-mock them: if AddCargoDryApplication didn't register
                // one, the handler must fail to activate here — that IS the bug class this guard catches. (RemoteCall
                // clients and CargoDry's own repos/Core infra are legitimately provided elsewhere → still mocked.)
                if (IsCrossModuleBridgeSeam(pt))
                    continue; // leave unregistered → activation fails if the module didn't provide it

                if (pt.IsInterface)
                {
                    services.AddScoped(pt, _ => Substitute.For(new[] { pt }, Array.Empty<object>()));
                    known.Add(pt);
                }
                else if (pt.IsClass && !pt.IsAbstract)
                {
                    services.AddScoped(pt);
                    known.Add(pt);
                    queue.Enqueue(pt);
                }
            }
        }

        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();

        var failures = new List<string>();
        foreach (var h in handlerTypes)
        {
            try
            {
                scope.ServiceProvider.GetRequiredService(h);
            }
            catch (Exception ex)
            {
                var root = ex;
                while (root.InnerException is not null) root = root.InnerException;
                failures.Add($"{h.FullName}: {root.GetType().Name}: {root.Message}");
            }
        }

        failures.Should().BeEmpty(
            "every CargoDry.Application handler must activate WITHOUT Payment.Application (split-host aizen-cargodry). " +
            "A failure here means a handler depends on a service whose only registration lives in another module's " +
            ".Application — the DI activation 911 class this guard exists to prevent.\n" +
            string.Join("\n", failures));

        // Every Payment bridge seam must resolve to a REAL HTTP remote impl in the split pod (no clean-fail fallback
        // remains — they were promoted). This is what makes the whole settlement/invoice chain functional split-host.
        scope.ServiceProvider.GetRequiredService<Aizen.Modules.Payment.Abstraction.Interface.ICargoDrySettlementPayoutService>()
            .Should().BeOfType<Services.RemoteCargoDrySettlementPayoutService>();
        scope.ServiceProvider.GetRequiredService<Aizen.Modules.Payment.Abstraction.Interface.ICargoDrySettlementPayoutLifecycleService>()
            .Should().BeOfType<Services.RemoteCargoDrySettlementPayoutLifecycleService>();
        scope.ServiceProvider.GetRequiredService<Aizen.Modules.Payment.Abstraction.Interface.ICargoDrySettlementInvoiceService>()
            .Should().BeOfType<Services.RemoteCargoDrySettlementInvoiceService>();
        scope.ServiceProvider.GetRequiredService<Aizen.Modules.Payment.Abstraction.Interface.ICargoDryRenewalInvoiceService>()
            .Should().BeOfType<Services.RemoteCargoDryRenewalInvoiceService>();
    }

    /// <summary>
    /// True for an interface declared in ANOTHER module's <c>*.Abstraction.Interface</c> namespace that is NOT a
    /// RemoteCall client — i.e. an in-process cross-module bridge service the split-host CargoDry pod must register
    /// itself (real remote impl or clean-fail fallback). These are intentionally left un-mocked so a missing
    /// registration surfaces as an activation failure (the bug class). RemoteCall clients (Starter-registered) and
    /// CargoDry's own / Core dependencies are not bridge seams and are mocked normally.
    /// </summary>
    private static bool IsCrossModuleBridgeSeam(Type pt)
        => pt.IsInterface
           && pt.Namespace is { } ns
           && ns.StartsWith("Aizen.Modules.", StringComparison.Ordinal)
           && !ns.StartsWith("Aizen.Modules.CargoDry", StringComparison.Ordinal)
           && ns.Contains(".Abstraction.Interface", StringComparison.Ordinal)
           && !typeof(Aizen.Core.RemoteCall.Abstraction.IAizenRemoteCall).IsAssignableFrom(pt);
}
