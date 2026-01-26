using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Aizen.Core.Realtime.Abstraction.Interfaces;
using Aizen.Core.Realtime.Abstraction.Models;

namespace Aizen.Core.Realtime.Services
{
    /// <summary>
    /// Domain-agnostic realtime publisher.
    /// Delegates actual delivery to ISocketManager which resolves domain-specific Hub contexts at runtime.
    /// </summary>
    public class SignalRRealtimePublisher : IRealtimePublisher
    {
        private readonly ISocketManager _socketManager;
        private readonly ILogger<SignalRRealtimePublisher> _logger;

        public SignalRRealtimePublisher(ISocketManager socketManager, ILogger<SignalRRealtimePublisher> logger)
        {
            _socketManager = socketManager ?? throw new ArgumentNullException(nameof(socketManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task PublishToChannelAsync(string channel, object payload, string? correlationId = null, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(channel))
                throw new ArgumentException("channel must be provided", nameof(channel));

            var msg = BuildMessage(payload, stream: channel, correlationId: correlationId);
            try
            {
                // channel treated as group name here
                return _socketManager.SendToGroupAsync(channel, msg, tenantId: null, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PublishToChannelAsync failed for channel {Channel}", channel);
                throw;
            }
        }

        public Task PublishToGroupAsync(string groupName, object payload, string? correlationId = null, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(groupName))
                throw new ArgumentException("groupName must be provided", nameof(groupName));

            var msg = BuildMessage(payload, stream: groupName, correlationId: correlationId);
            try
            {
                return _socketManager.SendToGroupAsync(groupName, msg, tenantId: null, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PublishToGroupAsync failed for group {Group}", groupName);
                throw;
            }
        }

        public Task PublishToUserAsync(string userId, object payload, string? correlationId = null, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("userId must be provided", nameof(userId));

            var msg = BuildMessage(payload, stream: $"user:{userId}", correlationId: correlationId);
            try
            {
                return _socketManager.SendToUserAsync(userId, msg, tenantId: null, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PublishToUserAsync failed for user {User}", userId);
                throw;
            }
        }

        private static RealtimeMessage BuildMessage(object payload, string stream, string? correlationId)
        {
            // Type can be inferred from payload type name (fallback). If caller passes a RealtimeMessage already, preserve.
            if (payload is RealtimeMessage rm)
            {
                // ensure stream/correlationId if missing
                var streamVal = string.IsNullOrWhiteSpace(rm.Stream) ? stream : rm.Stream;
                var corr = string.IsNullOrWhiteSpace(rm.CorrelationId) ? correlationId : rm.CorrelationId;
                return rm with { Stream = streamVal, CorrelationId = corr };
            }

            var typeName = payload?.GetType().Name ?? "unknown";

            return new RealtimeMessage
            {
                Type = typeName,
                Stream = stream,
                AggregateId = ExtractAggregateIdFromStream(stream),
                Payload = payload,
                CorrelationId = correlationId,
                CreatedAt = DateTimeOffset.UtcNow
            };
        }

        private static string ExtractAggregateIdFromStream(string stream)
        {
            if (string.IsNullOrWhiteSpace(stream)) return string.Empty;
            var idx = stream.IndexOf(':');
            if (idx < 0) return stream;
            return stream.Substring(idx + 1);
        }
    }
}