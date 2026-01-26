using System;
using System.Collections.Generic;

namespace Aizen.Core.Realtime.Abstraction.Models
{
    public record ConnectionMetadata
    {
        public IDictionary<string, string>? Items { get; init; }
        public string? RemoteIp { get; init; }
        public string? UserAgent { get; init; }
    }

    public record ConnectionInfo
    {
        public string ConnectionId { get; init; } = default!;
        public string? UserId { get; init; }
        public DateTimeOffset ConnectedAt { get; init; }
        public ConnectionMetadata? Metadata { get; init; }
    }
}