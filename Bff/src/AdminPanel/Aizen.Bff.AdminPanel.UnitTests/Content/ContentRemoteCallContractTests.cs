using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Modules.Content.Controllers.V1;
using FluentAssertions;
using MvcHttpMethodAttribute = Microsoft.AspNetCore.Mvc.Routing.HttpMethodAttribute;
using MvcRouteAttribute = Microsoft.AspNetCore.Mvc.RouteAttribute;
using RefitHttpMethodAttribute = Refit.HttpMethodAttribute;

namespace Aizen.Bff.AdminPanel.UnitTests.Content;

/// <summary>
/// Requirements #2 and #3 for IContentRemoteCall:
///  #2 — no method declares an `object` or `JsonElement` parameter/return type (the typed-body rule that
///       has broken this BFF before; must fail loudly on regression).
///  #3 — every route the client calls maps 1:1 (verb + path) to a route actually declared by the Content
///       module's controllers. Extracted by reflection from the module assembly, so a module route
///       rename/removal breaks these tests rather than production.
/// </summary>
public sealed class ContentRemoteCallContractTests
{
    // ── #2: typed bodies ────────────────────────────────────────────────────────────────────────
    [Fact]
    public void No_object_or_JsonElement_in_any_signature()
    {
        foreach (var method in typeof(IContentRemoteCall).GetMethods())
        {
            foreach (var p in method.GetParameters())
            {
                TypeTree(p.ParameterType).Should().NotContain(
                    t => t == typeof(object) || t == typeof(JsonElement),
                    $"{method.Name} parameter '{p.Name}' must be a typed Content contract, not object/JsonElement");
            }

            TypeTree(method.ReturnType).Should().NotContain(
                t => t == typeof(object) || t == typeof(JsonElement),
                $"{method.Name} return type must be a typed Content contract, not object/JsonElement");
        }
    }

    /// <summary>Flattens a (possibly generic) type into itself + all its generic argument types, recursively.</summary>
    private static IEnumerable<Type> TypeTree(Type t)
    {
        yield return t;
        if (t.IsGenericType)
            foreach (var arg in t.GetGenericArguments())
                foreach (var inner in TypeTree(arg))
                    yield return inner;
    }

    // ── #3: route drift ─────────────────────────────────────────────────────────────────────────
    [Fact]
    public void Every_remote_route_matches_a_declared_module_route()
    {
        var moduleRoutes = ModuleRoutes();
        moduleRoutes.Should().NotBeEmpty();

        var remoteRoutes = RemoteRoutes();
        remoteRoutes.Should().NotBeEmpty();

        foreach (var (verb, path) in remoteRoutes)
        {
            moduleRoutes.Should().Contain((verb, path),
                $"IContentRemoteCall calls {verb} {path}, which must be a real Content module controller route");
        }
    }

    private static HashSet<(string verb, string path)> ModuleRoutes()
    {
        var controllers = new[]
        {
            typeof(AdminContentController),
            typeof(AdminContentCommentsController),
            typeof(PublicContentController),
        };

        var routes = new HashSet<(string, string)>();
        foreach (var controller in controllers)
        {
            var prefix = controller.GetCustomAttributes<MvcRouteAttribute>(false).FirstOrDefault()?.Template ?? string.Empty;

            foreach (var method in controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            foreach (var http in method.GetCustomAttributes<MvcHttpMethodAttribute>(false))
            {
                var full = Normalize(Combine(prefix, http.Template));
                foreach (var verb in http.HttpMethods)
                    routes.Add((verb.ToUpperInvariant(), full));
            }
        }
        return routes;
    }

    private static IEnumerable<(string verb, string path)> RemoteRoutes()
    {
        foreach (var method in typeof(IContentRemoteCall).GetMethods())
        foreach (var http in method.GetCustomAttributes(true).OfType<RefitHttpMethodAttribute>())
            yield return (http.Method.Method.ToUpperInvariant(), Normalize(http.Path ?? string.Empty));
    }

    private static string Combine(string prefix, string? template) =>
        string.IsNullOrEmpty(template) ? prefix : $"{prefix.TrimEnd('/')}/{template.TrimStart('/')}";

    /// <summary>Trim slashes, lowercase, and collapse route params ({id},{slug},…) to {} so a param
    /// rename is not a false failure while a real segment rename IS caught.</summary>
    private static string Normalize(string path) =>
        Regex.Replace(path.Trim('/'), "\\{[^}]+\\}", "{}").ToLowerInvariant();
}
