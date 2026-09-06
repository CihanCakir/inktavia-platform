using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Authorization;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Core.Realtime.Abstraction.Interfaces;
using Aizen.Core.Realtime.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
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

    /// <summary>Phase-2 — per-SR live-trip group. Registered under the "trip" domain key
    /// (<c>AddDomainHub&lt;MobileRealtimeHub&gt;("trip")</c>). Unlike the per-user notification group, this group is
    /// client-named, so <see cref="JoinTrip"/> gates membership with an ownership check — the client can NOT join a
    /// trip it doesn't own.</summary>
    public static string TripGroup(long serviceRequestId) => $"trip:{serviceRequestId}";

    private readonly IParticipantProfileResolver _resolver;
    private readonly IParticipantIdentityHolder _identity;
    private readonly IServiceRequestRemoteCall _serviceRequest;
    private readonly ILogger<MobileRealtimeHub> _logger;

    public MobileRealtimeHub(
        IRealtimePublisher publisher,
        IParticipantProfileResolver resolver,
        IParticipantIdentityHolder identity,
        IServiceRequestRemoteCall serviceRequest,
        ILogger<MobileRealtimeHub> logger)
        : base(publisher)
    {
        _resolver = resolver;
        _identity = identity;
        _serviceRequest = serviceRequest;
        _logger = logger;
    }

    /// <summary>Subscribe this connection to a specific SR's live trip — ONLY after verifying the caller owns the SR.
    /// The client supplies the id, so ownership is checked server-side (resolve identity → EnsureOwnedAsync); a
    /// foreign/unknown id throws and the connection joins nothing. This is the deliberate, checked exception to the
    /// hub's "server decides the group" rule — required because a map screen watches one SR at a time.</summary>
    public async Task JoinTrip(long serviceRequestId)
    {
        await _resolver.ResolveAsync(Context.ConnectionAborted);
        var userId = _identity.UserId ?? 0;
        if (userId <= 0)
            throw new HubException("unauthorized");

        // Ownership check (same rule as the REST EnsureOwnedAsync gate): the module detail's OwnerUserId must equal the
        // resolved caller. A foreign/unknown id → not subscribed; existence is never leaked.
        long ownerUserId;
        try
        {
            var detail = (await _serviceRequest.GetDetail(serviceRequestId))?.Body?.Detail;
            ownerUserId = detail?.Request?.OwnerUserId ?? 0;
        }
        catch
        {
            ownerUserId = 0;
        }

        if (ownerUserId != userId)
        {
            _logger.LogWarning("mobile trip hub: user {UserId} denied join for SR {SrId}.", userId, serviceRequestId);
            throw new HubException("forbidden");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, TripGroup(serviceRequestId));
    }

    /// <summary>Unsubscribe from a trip group (e.g. leaving the map screen). No auth needed — removing yourself.</summary>
    public Task LeaveTrip(long serviceRequestId)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, TripGroup(serviceRequestId));

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
