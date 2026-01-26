using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Aizen.Core.Realtime.Abstraction.Interfaces;

namespace Aizen.Core.Realtime.Guards
{
    public class InMemoryRateLimitGuard : IRateLimitGuard
    {
        private readonly ConcurrentDictionary<string, (DateTime windowStart, int count)> _counters = new();
        private readonly int _maxPerMinute;
        private readonly int _maxMessageLength;

        public InMemoryRateLimitGuard(int maxPerMinute = 120, int maxMessageLength = 1000)
        {
            _maxPerMinute = maxPerMinute;
            _maxMessageLength = maxMessageLength;
        }

        public Task<bool> CheckRateLimitAsync(string connectionId)
        {
            var now = DateTime.UtcNow;
            var entry = _counters.GetOrAdd(connectionId, _ => (now, 0));
            if ((now - entry.windowStart).TotalSeconds > 60)
            {
                _counters[connectionId] = (now, 1);
                return Task.FromResult(true);
            }

            var newCount = entry.count + 1;
            if (newCount > _maxPerMinute) return Task.FromResult(false);

            _counters[connectionId] = (entry.windowStart, newCount);
            return Task.FromResult(true);
        }

        public bool IsMessageLengthAllowed(string message) => message?.Length <= _maxMessageLength;
    }
}