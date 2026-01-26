using System;

namespace Aizen.Core.Realtime.Abstraction.Models
{
    public record EventEnvelope
    {
        public long Sequence { get; init; }
        public string Stream { get; init; } = default!;
        public EventDto Event { get; init; } = default!;
        public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    }
}