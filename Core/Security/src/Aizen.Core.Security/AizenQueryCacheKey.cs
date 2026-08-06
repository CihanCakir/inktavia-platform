using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace Aizen.Core.Security;

/// <summary>
/// Canonical builder for the query read-cache keys produced by the CQRS query-handler
/// decorator. This is the single source of truth for the key format so that a write's
/// cache-eviction key always matches the read path's cache-storage key.
///
/// Key format: <c>"{handlerTypeName}:{sha256lowerhex(propString)}"</c>, where
/// <c>propString</c> is the concatenation of <c>"{PropName}_{Value}|"</c> for every
/// non-<c>QueryId</c> query property, in reflection (declaration) order. A query with no
/// hashable property yields the no-prop key <c>"{handlerTypeName}:"</c> (no hash).
///
/// The framework read path builds keys via <see cref="ForQuery"/> (reflection over the
/// query instance). Module cache-invalidation services, which do not hold a query
/// instance, build the SAME key via <see cref="For"/> (explicit property pairs) or
/// <see cref="FromPropString"/> (a pre-built propString) — never by hand-rolling their own
/// SHA256, which is how keys previously drifted out of sync with the read path.
/// </summary>
[SuppressMessage("ReSharper", "SuspiciousTypeConversion.Global")]
public static class AizenQueryCacheKey
{
    private const string QueryIdPropertyName = "QueryId";

    /// <summary>
    /// Canonical key for a query INSTANCE — used by the framework read path. Reflects over
    /// the query's properties exactly as the read-cache decorator does.
    /// </summary>
    public static string ForQuery(string handlerTypeName, object query)
        => Compose(handlerTypeName, BuildHash(query, null));

    /// <summary>
    /// Canonical key from explicit (name, value) property pairs — for invalidation services
    /// that do not hold a query instance. The pairs MUST match the query's non-<c>QueryId</c>
    /// properties, in declaration order (including paged/optional props with the same default
    /// values the read path uses), to reproduce the read key.
    /// </summary>
    public static string For(string handlerTypeName, params (string Name, object? Value)[] properties)
    {
        if (properties.Length == 0)
        {
            return Compose(handlerTypeName, string.Empty);
        }

        var sb = new StringBuilder();
        foreach (var (name, value) in properties)
        {
            _ = sb.AppendFormat(CultureInfo.CurrentCulture, "{0}_{1}|", name, value);
        }

        return Compose(handlerTypeName, AizenHash.ComputeHash(AizenHashType.Sha256, sb.ToString()));
    }

    /// <summary>
    /// Canonical key from a pre-built propString (the <c>"{Name}_{Value}|…"</c> concatenation,
    /// in declaration order). An empty propString yields the no-property key
    /// <c>"{handlerTypeName}:"</c>.
    /// </summary>
    public static string FromPropString(string handlerTypeName, string propString)
        => Compose(handlerTypeName,
            string.IsNullOrEmpty(propString)
                ? string.Empty
                : AizenHash.ComputeHash(AizenHashType.Sha256, propString));

    private static string Compose(string handlerTypeName, string hashOrEmpty)
        => $"{handlerTypeName}:{hashOrEmpty}";

    // Mirrors the read-path reflection algorithm; the CQRS query decorator delegates here so
    // read keys and invalidation keys are produced by one implementation.
    private static string BuildHash(object obj, string? propName)
    {
        var sb = new StringBuilder();
        if (obj.GetType().IsValueType || obj is string)
        {
            _ = sb.AppendFormat(CultureInfo.CurrentCulture, "{0}_{1}|", propName, obj);
        }
        else if (obj.GetType().GetProperties().Count(x => x.Name != QueryIdPropertyName) == 0)
        {
            return string.Empty;
        }
        else
        {
            foreach (var prop in obj.GetType().GetProperties())
            {
                if (prop.Name == QueryIdPropertyName)
                {
                    continue;
                }

                if (typeof(IEnumerable<object>).IsAssignableFrom(prop.PropertyType))
                {
                    var get = prop.GetGetMethod()!;
                    if (!get.IsStatic && get.GetParameters().Length == 0)
                    {
                        var collection = (IEnumerable<object>)get.Invoke(obj, null)!;
                        foreach (var o in collection)
                        {
                            _ = sb.Append(BuildHash(o, prop.Name));
                        }
                    }
                }
                else
                {
                    _ = sb.AppendFormat(CultureInfo.CurrentCulture, "{0}{1}_{2}|", propName, prop.Name,
                        prop.GetValue(obj, null));
                }
            }
        }

        return AizenHash.ComputeHash(AizenHashType.Sha256, sb.ToString());
    }
}
