using System;

namespace Aizen.Core.Realtime.Abstraction.Configuration
{
    public class RealtimeOptions
    {
        public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
        public bool UseRedisBackplane { get; set; } = false;
        public string? RedisConnectionString { get; set; }
        public string HubPath { get; set; } = "/hubs/events";
        public int KeepAliveIntervalSeconds { get; set; } = 15;
        public int ClientTimeoutSeconds { get; set; } = 30;
        public int? MaximumReceiveMessageSize { get; set; }
    }
}