using System;
using System.Collections.Generic;

namespace Aizen.Core.Realtime.Abstraction.Models
{
    public record EventDto
    {
        public string Id { get; init; } = Guid.NewGuid().ToString();
        public string AggregateId { get; init; } = default!;
        public string Type { get; init; } = default!;
        public object? Data { get; init; }
        public IDictionary<string, string>? Metadata { get; init; }
        public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
        public long Version { get; init; }
    }
}