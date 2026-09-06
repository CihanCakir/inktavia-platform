using Aizen.Core.Realtime.Abstraction.Interfaces;
using Aizen.Core.Realtime.Abstraction.Models;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Abstraction.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Message;

namespace Aizen.Bff.Marine.Participant.Mobile.Realtime;

/// <summary>
/// BE-MO9b — the single per-surface routing declaration (ADR layer-2): the Notification module's
/// <see cref="NotificationSentMessage"/> → a thin realtime frame + the recipient's per-user group. The owner bell
/// subscribes to this ONE canonical event and inherits the whole owner event set (offers, payment, completion,
/// dispute, change-order, maintenance-N2, price-change) for free — the Notification module already fans every owner
/// notification through it.
///
/// COST-FREE by construction: the frame carries only the notification id + type name + title + the deep-link ref
/// (<c>ReferenceType</c>/<c>ReferenceId</c>) + <c>SentAt</c>. No amounts / commission / net / margin / provider ids.
///
/// Filtering (drop at <see cref="Map"/> by returning null — an empty <see cref="GetTargets"/> would BROADCAST, not
/// skip): fire only for the <b>InApp</b> channel (the bell is the in-app surface; Push is MO9a) and a real
/// <c>RecipientUserId</c>. <see cref="Unwrap"/> accepts both the raw message and the <c>EventDto</c>-wrapped form the
/// generic realtime consumer emits.
/// </summary>
public sealed class MobileNotificationEventSocketMapper : IEventSocketMapper
{
    public RealtimeMessage? Map(object domainEvent)
    {
        // One mapper per BFF (the ingress resolves a single IEventSocketMapper) — branch by message type.
        if (Resolve(domainEvent) is { } n)
            return new RealtimeMessage
            {
                Type        = MobileRealtimeNotificationTypes.FrameType,
                Stream      = MobileRealtimeHub.UserGroup(n.RecipientUserId),
                AggregateId = n.NotificationId.ToString(),
                Payload     = new MobileRealtimeNotification(
                    NotificationId: n.NotificationId,
                    Type:           n.Type.ToString(),
                    Title:          n.Title,
                    ReferenceType:  n.ReferenceType,
                    ReferenceId:    n.ReferenceId,
                    SentAt:         n.SentAt),
            };

        if (ResolveTrip(domainEvent) is { } t)
            return new RealtimeMessage
            {
                Type        = MobileTripEventTypes.FrameType,
                Stream      = MobileRealtimeHub.TripGroup(t.ServiceRequestId), // group broadcast key
                AggregateId = t.ServiceRequestId.ToString(),
                Payload     = new MobileTripFrame(
                    ServiceRequestId: t.ServiceRequestId,
                    Event:            t.EventType.ToString(),
                    Status:           t.Status.ToString(),
                    Latitude:         (double?)t.Latitude,
                    Longitude:        (double?)t.Longitude,
                    Heading:          (double?)t.Heading,
                    PingAt:           t.PingAt,
                    EtaMinutes:       t.EtaMinutes,
                    StartedAt:        t.StartedAt),
            };

        return null; // drop — an empty GetTargets would BROADCAST, so unknown events must be dropped here
    }

    public (IEnumerable<string> UserIds, IEnumerable<string> GroupNames) GetTargets(object domainEvent)
    {
        if (Resolve(domainEvent) is { } n)
            // Per-recipient only: the target user's connections. Never a shared group, never client-supplied targets.
            return (Array.Empty<string>(), new[] { MobileRealtimeHub.UserGroup(n.RecipientUserId) });

        if (ResolveTrip(domainEvent) is { } t)
            // The per-SR trip group. Only owners who passed JoinTrip's ownership check are members.
            return (Array.Empty<string>(), new[] { MobileRealtimeHub.TripGroup(t.ServiceRequestId) });

        return (Array.Empty<string>(), Array.Empty<string>());
    }

    private static TripRealtimeMessage? ResolveTrip(object domainEvent) => domainEvent switch
    {
        TripRealtimeMessage t                    => t,
        EventDto { Data: TripRealtimeMessage d }  => d,
        _                                         => null,
    };

    /// <summary>Unwrap + filter in ONE place so <see cref="Map"/> and <see cref="GetTargets"/> agree: the bell fires
    /// only for an in-app notification addressed to a real recipient; anything else returns null → dropped.</summary>
    private static NotificationSentMessage? Resolve(object domainEvent)
    {
        if (Unwrap(domainEvent) is not { } n) return null;
        if (n.Channel != NotificationChannel.InApp) return null;   // the bell is the in-app surface; Push = MO9a
        if (n.RecipientUserId <= 0) return null;
        return n;
    }

    // The generic RealtimeEventConsumer wraps the bus message in an EventDto (Data = the message); accept both the
    // wrapped and raw forms, and only our known message type (strict — an unrelated Data must not target anyone).
    private static NotificationSentMessage? Unwrap(object domainEvent) => domainEvent switch
    {
        NotificationSentMessage n                    => n,
        EventDto { Data: NotificationSentMessage d }  => d,
        _                                             => null,
    };
}

/// <summary>The thin, cost-free realtime frame payload for the owner bell — enough to refetch the badge/list and
/// deep-link, and nothing more. No body content, no economics.</summary>
public sealed record MobileRealtimeNotification(
    long NotificationId,
    string Type,
    string Title,
    string? ReferenceType,
    long? ReferenceId,
    DateTimeOffset SentAt);

/// <summary>Realtime frame/event constants for the owner bell (mirrors <c>ProviderRealtimeEvent</c>).</summary>
public static class MobileRealtimeNotificationTypes
{
    /// <summary>The SignalR frame <c>Type</c> the client listens for.</summary>
    public const string FrameType = "mobileNotification";
}

/// <summary>Phase-2 — the cost-free live-trip frame for the owner map: coarse position + a straight-line ETA. No
/// economics, no user/provider ids. <c>Event</c> is Started|Location|Arrived|Cancelled; <c>Status</c> is the trip
/// status name.</summary>
public sealed record MobileTripFrame(
    long ServiceRequestId,
    string Event,
    string Status,
    double? Latitude,
    double? Longitude,
    double? Heading,
    DateTime? PingAt,
    double? EtaMinutes,
    DateTime? StartedAt);

public static class MobileTripEventTypes
{
    /// <summary>The SignalR frame <c>Type</c> the map screen listens for (delivered over "ReceiveEvent").</summary>
    public const string FrameType = "tripEvent";
}
