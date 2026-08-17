using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Authorization;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Core.Realtime.Abstraction.Interfaces;
using Aizen.Core.Realtime.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.Marine.Participant.Mobile.Realtime;

/// <summary>
/// BE-MO9b — the BFF-hosted realtime edge for the owner app's live notification bell (ADR: BFF-hosted edge on
/// Aizen.Core.Realtime, modules publish-only). Mirrors <c>AdminNotificationHub</c>: a notification belongs to exactly
/// one user, so its live frame reaches only that user's connections via a PER-RECIPIENT group.
///
/// Security boundary — the server decides the group, never the client:
/// - <c>[Authorize(ParticipantAuthenticated)]</c> requires a verified Keycloak participant token.
/// - On connect the server resolves the participant's numeric Identity user id (via the same by-subject resolver used
///   to assert the caller to modules) and joins ONLY <see cref="UserGroup"/> for that id. It fails CLOSED
///   (<c>Context.Abort()</c>) if the id can't be resolved — never a shared/"public" group.
/// - There are NO client-callable join/subscribe methods, so a client can only ever observe their own
///   <c>mobile-notification:{ownId}</c> group.
/// </summary>
[Authorize(Policy = ParticipantAuthorizationPolicies.ParticipantAuthenticated)]
public sealed class MobileRealtimeHub : DomainHubBase
{
    /// <summary>
    /// Every notification addressed to one recipient. NOTE: the prefix (up to the first ':') MUST equal the domain key
    /// registered via <c>AddDomainHub&lt;MobileRealtimeHub&gt;("mobile-notification")</c> — the framework's socket
    /// manager routes a group broadcast to a hub by parsing that prefix.
    /// </summary>
    public static string UserGroup(long recipientUserId) => $"mobile-notification:{recipientUserId}";

    private readonly IParticipantProfileResolver _resolver;
    private readonly IParticipantIdentityHolder _identity;
    private readonly ILogger<MobileRealtimeHub> _logger;

    public MobileRealtimeHub(
        IRealtimePublisher publisher,
        IParticipantProfileResolver resolver,
        IParticipantIdentityHolder identity,
        ILogger<MobileRealtimeHub> logger)
        : base(publisher)
    {
        _resolver = resolver;
        _identity = identity;
        _logger = logger;
    }

    public override string DomainName => "mobile-notification";

    public override async Task OnConnectedAsync()
    {
        // Resolve the participant's numeric Identity user id from the verified Keycloak subject (server-side only).
        await _resolver.ResolveAsync(Context.ConnectionAborted);
        var userId = _identity.UserId ?? 0;

        if (userId <= 0)
        {
            // Fail closed: an unresolved connection joins nothing and is dropped — it must never fall back to a
            // shared/"public" group where it could observe another participant's notifications.
            _logger.LogWarning("mobile-notification hub: participant user id could not be resolved — aborting connection.");
            Context.Abort();
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId));
        await base.OnConnectedAsync();
    }
}
