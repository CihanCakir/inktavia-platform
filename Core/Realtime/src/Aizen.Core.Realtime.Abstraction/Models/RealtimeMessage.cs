using System;

namespace Aizen.Core.Realtime.Abstraction.Models
{
    public record RealtimeMessage
    {
        public string Type { get; init; } = default!;
        public string Stream { get; init; } = default!;
        public string AggregateId { get; init; } = default!;
        public object? Payload { get; init; }
        public string? CorrelationId { get; init; }
        public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    }
}