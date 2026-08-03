using Aizen.Core.Realtime.Abstraction.Interfaces;
using Aizen.Core.Realtime.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.AdminPanel.Realtime;

/// <summary>
/// BFF-hosted realtime edge for admin messaging (ADR: BFF-hosted edge on Aizen.Core.Realtime,
/// modules publish-only). Built on the framework's <see cref="DomainHubBase"/> — no hand-rolled hub.
///
/// Security boundary (server decides groups, never the client):
/// - <c>[Authorize(Policy = "AdminPanelAccess")]</c> requires an authenticated Keycloak admin (role "Admin").
/// - On connect the server adds the connection to the single <see cref="AdminGroup"/> (an admin observes all
///   conversations). Fails closed (<c>Context.Abort()</c>) if identity/role can't be confirmed.
/// - There are NO client-callable join/subscribe methods — clients cannot pick their groups.
/// </summary>
[Authorize(Policy = "AdminPanelAccess")]
public sealed class AdminMessagingHub : DomainHubBase
{
    /// <summary>
    /// The single admin observation group. NOTE: the group name's prefix (up to the first ':') must equal the
    /// domain key registered via <c>AddDomainHub&lt;AdminMessagingHub&gt;("admin-messaging")</c>, because the
    /// framework's socket manager routes a group broadcast to a hub by parsing that prefix. Hence
    /// "admin-messaging:all", not "admin:messaging".
    /// </summary>
    public const string AdminGroup = "admin-messaging:all";

    private readonly ILogger<AdminMessagingHub> _logger;

    public AdminMessagingHub(IRealtimePublisher publisher, ILogger<AdminMessagingHub> logger)
        : base(publisher)
        => _logger = logger;

    public override string DomainName => "admin-messaging";

    public override async Task OnConnectedAsync()
    {
        var user = Context.User;
        if (user?.Identity?.IsAuthenticated != true || !user.IsInRole("Admin"))
        {
            _logger.LogWarning("admin-messaging hub: admin identity/role could not be confirmed — aborting connection.");
            Context.Abort();
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, AdminGroup);
        await base.OnConnectedAsync();
    }
}
