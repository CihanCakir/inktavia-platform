using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aizen.Core.Realtime.Abstraction.Models;

namespace Aizen.Core.Realtime.Abstraction.Interfaces
{
    public interface ISocketManager
    {
        Task OnConnectedAsync(string connectionId, string? userId, string? tenantId = null, ConnectionMetadata? metadata = null, CancellationToken ct = default);
        Task OnDisconnectedAsync(string connectionId, string? userId, CancellationToken ct = default);

        Task SendToConnectionAsync(string connectionId, RealtimeMessage message, string? tenantId = null, CancellationToken ct = default);
        Task SendToUserAsync(string userId, RealtimeMessage message, string? tenantId = null, CancellationToken ct = default);
        Task SendToGroupAsync(string groupName, RealtimeMessage message, string? tenantId = null, CancellationToken ct = default);
        Task BroadcastAsync(RealtimeMessage message, string? tenantId = null, CancellationToken ct = default);

        Task AddToGroupAsync(string connectionId, string groupName, CancellationToken ct = default);
        Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken ct = default);
        IEnumerable<string> GetGroupsForConnection(string connectionId);
        IEnumerable<string> GetConnectionsInGroup(string groupName);

        Task SetPresenceAsync(string connectionId, PresenceState state, CancellationToken ct = default);
        Task<PresenceState?> GetPresenceAsync(string connectionId, CancellationToken ct = default);
        IEnumerable<ConnectionInfo> GetConnectionsByUser(string userId);
        ConnectionInfo? GetConnectionInfo(string connectionId);

        Task UpdateConnectionMetadataAsync(string connectionId, ConnectionMetadata metadata, CancellationToken ct = default);

        Func<string, RealtimeMessage, Task>? OnDeliveryFailure { get; set; }
        void RegisterMessageFilter(string? tenantId, Func<RealtimeMessage, Task<RealtimeMessage?>> filter);
        Func<string, RealtimeMessage, Task<bool>>? RateLimitCheckAsync { get; set; }
        void RegisterTenantPolicy(string tenantId, TenantRealtimePolicy policy);
        Task FlushAsync(CancellationToken ct = default);
    }

    public enum PresenceState
    {
        Online,
        Away,
        Offline,
        Unknown
    }

    public record TenantRealtimePolicy
    {
        public int MaxMessagesPerMinute { get; init; } = 1000;
        public int MaxConnections { get; init; } = 500;
    }
}