using Aizen.Core.Realtime.Abstraction.Interfaces;
using Aizen.Core.Realtime.Abstraction.Models;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Abstraction.Message;

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
        var n = Resolve(domainEvent);
        if (n is null) return null;

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
    }

    public (IEnumerable<string> UserIds, IEnumerable<string> GroupNames) GetTargets(object domainEvent)
    {
        var n = Resolve(domainEvent);
        return n is null
            ? (Array.Empty<string>(), Array.Empty<string>())
            // Per-recipient only: the target user's connections. Never a shared group, never client-supplied targets.
            : (Array.Empty<string>(), new[] { MobileRealtimeHub.UserGroup(n.RecipientUserId) });
    }

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
