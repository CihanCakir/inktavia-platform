using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Aizen.Core.Realtime.Abstraction.Domain;
/// <summary>
/// Global, thread-safe registry for domain -> event name sets.
/// Register domain events at startup (e.g. "activity" domain registers its events).
/// Consumers/hubs/publishers can query registered events.
/// </summary>
public static class RealtimeEventRegistry
{
    private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _map
        = new();

    public static void RegisterDomainEvents(string domain, params string[] eventNames)
    {
        var set = _map.GetOrAdd(domain, _ => new ConcurrentDictionary<string, byte>());
        foreach (var e in eventNames ?? Enumerable.Empty<string>())
            set[e] = 0;
    }

    public static IEnumerable<string> GetDomainEvents(string domain)
    {
        if (_map.TryGetValue(domain, out var set))
            return set.Keys;
        return Enumerable.Empty<string>();
    }

    public static bool IsEventRegistered(string domain, string eventName)
        => _map.TryGetValue(domain, out var set) && set.ContainsKey(eventName);
}