using System;
using System.Collections.Concurrent;

namespace Aizen.Core.Realtime.Domain
{
    /// <summary>
    /// Domain -> Hub type registry.
    /// Modules call AddDomainHub<T> during startup to register their hub.
    /// </summary>
    public static class DomainHubRegistry
    {
        private static readonly ConcurrentDictionary<string, Type> _map = new();

        public static void RegisterDomainHub(string domainKey, Type hubType)
        {
            if (string.IsNullOrWhiteSpace(domainKey)) throw new ArgumentNullException(nameof(domainKey));
            if (hubType == null) throw new ArgumentNullException(nameof(hubType));
            _map[domainKey] = hubType;
        }

        public static bool TryGetDomainHub(string domainKey, out Type? hubType)
            => _map.TryGetValue(domainKey, out hubType);
    }
}