using Aizen.Bff.AdminPanel.Application.Common.Services;
using Aizen.Core.Realtime.Abstraction.Interfaces;
using Aizen.Core.Realtime.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.AdminPanel.Realtime;

/// <summary>
/// BFF-hosted realtime edge for the admin notification badge (ADR: BFF-hosted edge on Aizen.Core.Realtime, modules
/// publish-only). Mirrors <see cref="AdminMessagingHub"/> but targets a PER-RECIPIENT group instead of one admin
/// group — a notification belongs to exactly one user, so its live frame must reach only that user's connections.
///
/// Why this replaces the old <c>/hubs/notification</c>: that hub lived on the Notification MODULE, which the browser
/// cannot reach (modules are internal; the BFF is the only public edge) — the SPA's connection 404'd and the badge
/// only refreshed on a manual reload. Same class of fix as admin-messaging Wave 1.
///
/// Security boundary (server decides the group, never the client):
/// - <c>[Authorize(Policy = "AdminPanelAccess")]</c> requires an authenticated Keycloak admin.
/// - On connect the server resolves the admin's numeric Identity user id (via <see cref="IAdminIdentityResolver"/>,
///   the same by-subject resolution used to assert the acting admin to modules) and joins ONLY
///   <see cref="UserGroup"/> for that id. Fails closed (<c>Context.Abort()</c>) if the id can't be resolved.
/// - There are NO client-callable join/subscribe methods — a client cannot join another user's group.
/// </summary>
[Authorize(Policy = "AdminPanelAccess")]
public sealed class AdminNotificationHub : DomainHubBase
{
    /// <summary>
    /// Every notification addressed to one recipient. NOTE: the prefix (up to the first ':') must equal the domain
    /// key registered via <c>AddDomainHub&lt;AdminNotificationHub&gt;("admin-notification")</c>, because the
    /// framework's socket manager routes a group broadcast to a hub by parsing that prefix.
    /// </summary>
    public static string UserGroup(long recipientUserId) => $"admin-notification:{recipientUserId}";

    private readonly IAdminIdentityResolver _resolver;
    private readonly IAdminIdentityHolder _identity;
    private readonly ILogger<AdminNotificationHub> _logger;

    public AdminNotificationHub(
        IRealtimePublisher publisher,
        IAdminIdentityResolver resolver,
        IAdminIdentityHolder identity,
        ILogger<AdminNotificationHub> logger)
        : base(publisher)
    {
        _resolver = resolver;
        _identity = identity;
        _logger = logger;
    }

    public override string DomainName => "admin-notification";

    public override async Task OnConnectedAsync()
    {
        // Resolve the acting admin's numeric Identity user id from the verified Keycloak subject (server-side only).
        await _resolver.ResolveAsync(Context.ConnectionAborted);
        var userId = _identity.UserId ?? 0;

        if (userId <= 0)
        {
            // Fail closed: an unresolved connection joins nothing and is dropped. It must never fall back to a
            // shared or "public" group where it could observe another user's notifications.
            _logger.LogWarning("admin-notification hub: admin user id could not be resolved — aborting connection.");
            Context.Abort();
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId));
        await base.OnConnectedAsync();
    }
}
