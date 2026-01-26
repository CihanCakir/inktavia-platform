using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Aizen.Core.Realtime.Abstraction.Interfaces;
using Aizen.Core.Realtime.Abstraction.Models;
using Aizen.Core.Realtime.Domain;

namespace Aizen.Core.Realtime.Services
{
    /// <summary>
    /// Generic SignalR-backed socket manager. Resolves Hub contexts at runtime using DomainHubRegistry and IServiceProvider.
    /// </summary>
    public class SignalRSocketManager : ISocketManager
    {
        private readonly IServiceProvider _sp;
        private readonly ConcurrentDictionary<string, ConnectionInfo> _connections = new();
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _userConnections = new();
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _groups = new();
#pragma warning disable CS8714 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'notnull' constraint.
        private readonly ConcurrentDictionary<string?, List<Func<RealtimeMessage, Task<RealtimeMessage?>>>> _filters = new();
#pragma warning restore CS8714 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'notnull' constraint.

        public Func<string, RealtimeMessage, Task>? OnDeliveryFailure { get; set; }
        public Func<string, RealtimeMessage, Task<bool>>? RateLimitCheckAsync { get; set; }

        public SignalRSocketManager(IServiceProvider sp) => _sp = sp ?? throw new ArgumentNullException(nameof(sp));

        public Task OnConnectedAsync(string connectionId, string? userId, string? tenantId = null, ConnectionMetadata? metadata = null, CancellationToken ct = default)
        {
            var info = new ConnectionInfo { ConnectionId = connectionId, UserId = userId, ConnectedAt = DateTimeOffset.UtcNow, Metadata = metadata };
            _connections[connectionId] = info;
            if (!string.IsNullOrEmpty(userId))
            {
                var set = _userConnections.GetOrAdd(userId!, _ => new ConcurrentDictionary<string, byte>());
                set[connectionId] = 0;
            }
            return Task.CompletedTask;
        }

        public Task OnDisconnectedAsync(string connectionId, string? userId, CancellationToken ct = default)
        {
            _connections.TryRemove(connectionId, out _);
            if (!string.IsNullOrEmpty(userId) && _userConnections.TryGetValue(userId!, out var set))
            {
                set.TryRemove(connectionId, out _);
                if (set.IsEmpty) _userConnections.TryRemove(userId!, out _);
            }
            foreach (var kv in _groups) kv.Value.TryRemove(connectionId, out _);
            return Task.CompletedTask;
        }

        private object? ResolveHubContextForDomain(string domainKey)
        {
            if (!DomainHubRegistry.TryGetDomainHub(domainKey, out var hubType) || hubType == null) return null;
            var hubContextType = typeof(IHubContext<>).MakeGenericType(hubType);
            return _sp.GetService(hubContextType);
        }

        private Task InvokeGroupSendAsync(object hubContext, string groupName, string method, object payload, CancellationToken ct)
        {
            if (hubContext == null) return Task.CompletedTask;
            dynamic ctx = hubContext;
            return (Task)ctx.Clients.Group(groupName).SendAsync(method, payload, ct);
        }

        private Task InvokeUserSendAsync(object hubContext, string userId, string method, object payload, CancellationToken ct)
        {
            if (hubContext == null) return Task.CompletedTask;
            dynamic ctx = hubContext;
            return (Task)ctx.Clients.User(userId).SendAsync(method, payload, ct);
        }

        private Task InvokeClientSendAsync(object hubContext, string connectionId, string method, object payload, CancellationToken ct)
        {
            if (hubContext == null) return Task.CompletedTask;
            dynamic ctx = hubContext;
            return (Task)ctx.Clients.Client(connectionId).SendAsync(method, payload, ct);
        }

        public async Task SendToConnectionAsync(string connectionId, RealtimeMessage message, string? tenantId = null, CancellationToken ct = default)
        {
            try
            {
                var msg = await RunFiltersAsync(tenantId, message);
                if (msg == null) return;

                if (RateLimitCheckAsync != null)
                {
                    var ok = await RateLimitCheckAsync(connectionId, msg);
                    if (!ok) return;
                }

                var domain = ParseDomainFromStream(msg.Stream);
                var hubCtx = ResolveHubContextForDomain(domain);
                if (hubCtx != null) { await InvokeClientSendAsync(hubCtx, connectionId, "ReceiveEvent", msg, ct); return; }
            }
            catch (Exception) { if (OnDeliveryFailure != null) await OnDeliveryFailure(connectionId, message); }
        }

        public async Task SendToUserAsync(string userId, RealtimeMessage message, string? tenantId = null, CancellationToken ct = default)
        {
            try
            {
                var msg = await RunFiltersAsync(tenantId, message);
                if (msg == null) return;

                var domain = ParseDomainFromStream(msg.Stream);
                var hubCtx = ResolveHubContextForDomain(domain);
                if (hubCtx != null) { await InvokeUserSendAsync(hubCtx, userId, "ReceiveEvent", msg, ct); return; }
            }
            catch (Exception) { }
        }

        public async Task SendToGroupAsync(string groupName, RealtimeMessage message, string? tenantId = null, CancellationToken ct = default)
        {
            try
            {
                var msg = await RunFiltersAsync(tenantId, message);
                if (msg == null) return;

                var domain = ParseDomainFromStream(groupName);
                var hubCtx = ResolveHubContextForDomain(domain);
                if (hubCtx != null) { await InvokeGroupSendAsync(hubCtx, groupName, "ReceiveEvent", msg, ct); return; }
            }
            catch (Exception) { }
        }

        public Task BroadcastAsync(RealtimeMessage message, string? tenantId = null, CancellationToken ct = default)
        {
            var domain = ParseDomainFromStream(message.Stream);
            var hubCtx = ResolveHubContextForDomain(domain);
            if (hubCtx != null) return InvokeGroupSendAsync(hubCtx, message.Stream, "ReceiveEvent", message, ct);
            return Task.CompletedTask;
        }

        public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken ct = default)
        {
            var set = _groups.GetOrAdd(groupName, _ => new ConcurrentDictionary<string, byte>());
            set[connectionId] = 0;

            var domain = ParseDomainFromStream(groupName);
            var hubCtx = ResolveHubContextForDomain(domain);
            if (hubCtx != null)
            {
                dynamic ctx = hubCtx;
                return (Task)ctx.Groups.AddToGroupAsync(connectionId, groupName);
            }
            return Task.CompletedTask;
        }

        public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken ct = default)
        {
            if (_groups.TryGetValue(groupName, out var set)) set.TryRemove(connectionId, out _);

            var domain = ParseDomainFromStream(groupName);
            var hubCtx = ResolveHubContextForDomain(domain);
            if (hubCtx != null)
            {
                dynamic ctx = hubCtx;
                return (Task)ctx.Groups.RemoveFromGroupAsync(connectionId, groupName);
            }
            return Task.CompletedTask;
        }

        public IEnumerable<string> GetGroupsForConnection(string connectionId)
            => _groups.Where(kv => kv.Value.ContainsKey(connectionId)).Select(kv => kv.Key);

        public IEnumerable<string> GetConnectionsInGroup(string groupName)
            => _groups.TryGetValue(groupName, out var set) ? set.Keys.ToArray() : Array.Empty<string>();

        public Task SetPresenceAsync(string connectionId, PresenceState state, CancellationToken ct = default)
        {
            if (_connections.TryGetValue(connectionId, out var info))
            {
                var meta = info.Metadata ?? new ConnectionMetadata();
                var items = meta.Items ?? new Dictionary<string, string>();
                items["presence"] = state.ToString();
                var newMeta = meta with { Items = items };
                _connections[connectionId] = info with { Metadata = newMeta };
            }
            return Task.CompletedTask;
        }

        public Task<PresenceState?> GetPresenceAsync(string connectionId, CancellationToken ct = default)
        {
            if (_connections.TryGetValue(connectionId, out var info) && info.Metadata?.Items != null && info.Metadata.Items.TryGetValue("presence", out var s))
            {
                if (Enum.TryParse<PresenceState>(s, out var state)) return Task.FromResult<PresenceState?>(state);
            }
            return Task.FromResult<PresenceState?>(null);
        }

        public IEnumerable<ConnectionInfo> GetConnectionsByUser(string userId)
        {
            if (_userConnections.TryGetValue(userId, out var set))
            {
                return set.Keys.Select(id => _connections.TryGetValue(id, out var info) ? info : null).Where(x => x != null)!;
            }
            return Array.Empty<ConnectionInfo>();
        }

        public ConnectionInfo? GetConnectionInfo(string connectionId)
        {
            _connections.TryGetValue(connectionId, out var info);
            return info;
        }

        public Task UpdateConnectionMetadataAsync(string connectionId, ConnectionMetadata metadata, CancellationToken ct = default)
        {
            if (_connections.TryGetValue(connectionId, out var info))
            {
                _connections[connectionId] = info with { Metadata = metadata };
            }
            return Task.CompletedTask;
        }

        public Task FlushAsync(CancellationToken ct = default) => Task.CompletedTask;

        public void RegisterMessageFilter(string? tenantId, Func<RealtimeMessage, Task<RealtimeMessage?>> filter)
        {
            var list = _filters.GetOrAdd(tenantId, _ => new List<Func<RealtimeMessage, Task<RealtimeMessage?>>>());
            list.Add(filter);
        }

        public void RegisterTenantPolicy(string tenantId, Aizen.Core.Realtime.Abstraction.Interfaces.TenantRealtimePolicy policy)
        {
            // store tenant policy if needed (not implemented)
        }

        private async Task<RealtimeMessage?> RunFiltersAsync(string? tenantId, RealtimeMessage message)
        {
            if (_filters.TryGetValue(tenantId, out var list))
            {
                var current = message;
                foreach (var f in list)
                {
                    current = await f(current);
                    if (current == null) return null;
                }
                return current;
            }
            if (_filters.TryGetValue(null, out var global))
            {
                var current = message;
                foreach (var f in global)
                {
                    current = await f(current);
                    if (current == null) return null;
                }
                return current;
            }
            return message;
        }

        private static string ParseDomainFromStream(string stream)
        {
            if (string.IsNullOrWhiteSpace(stream)) return string.Empty;
            var idx = stream.IndexOf(':');
            return idx > 0 ? stream.Substring(0, idx) : stream;
        }
    }
}