using Aizen.Core.Realtime.Abstraction.Interfaces;
using Aizen.Core.Realtime.Abstraction.Models;
using Aizen.Modules.Messaging.Abstraction.Message;
using Aizen.Modules.Notification.Abstraction.Message;

namespace Aizen.Bff.AdminPanel.Realtime;

/// <summary>
/// The single per-surface routing declaration (ADR layer-2): "this module bus event → this thin frame + these
/// target groups". The framework's <c>RealtimeIngressService</c> resolves one <see cref="IEventSocketMapper"/>
/// and calls <see cref="Map"/> + <see cref="GetTargets"/> for each consumed event.
///
/// Handles three module bus events as THIN frames (no content — the SPA refetches over HTTP), discriminated by the
/// envelope <c>Type</c> + inner payload <c>type</c>. The framework resolves ONE <see cref="IEventSocketMapper"/> per
/// ingress, so every surface this BFF broadcasts is broadened into this single mapper rather than registered
/// separately (ADR layer-2). Routing differs per event:
///   • <see cref="MessagingMessageSentMessage"/>     → "messagingEvent"    → single admin group (an admin observes all)
///   • <see cref="MessagingModerationEventMessage"/> → "messagingEvent"    → single admin group
///   • <see cref="NotificationSentMessage"/>         → "notificationEvent" → the recipient's PER-USER group
///     (a notification belongs to one user; its live badge frame must reach only that user's connections).
/// </summary>
public sealed class AdminMessagingEventSocketMapper : IEventSocketMapper
{
    public RealtimeMessage? Map(object domainEvent) => Unwrap(domainEvent) switch
    {
        MessagingMessageSentMessage m => new RealtimeMessage
        {
            Type        = "messagingEvent",
            Stream      = AdminMessagingHub.AdminGroup,
            AggregateId = m.ConversationId.ToString(),
            Payload     = new { type = "MessageAdded", conversationId = m.ConversationId },
        },
        MessagingModerationEventMessage e => new RealtimeMessage
        {
            Type        = "messagingEvent",
            Stream      = AdminMessagingHub.AdminGroup,
            AggregateId = e.ConversationId.ToString(),
            Payload     = new
            {
                type           = "ModerationEvent",
                conversationId = e.ConversationId,
                messageId      = e.MessageId,
                kind           = e.Kind,
                newStatus      = e.NewStatus,
            },
        },
        NotificationSentMessage n => new RealtimeMessage
        {
            Type        = "notificationEvent",
            Stream      = AdminNotificationHub.UserGroup(n.RecipientUserId),
            AggregateId = n.NotificationId.ToString(),
            // Thin: enough to refetch the badge/list and (optionally) deep-link. No body/content on the wire.
            Payload     = new
            {
                type           = "NotificationCreated",
                notificationId = n.NotificationId,
                referenceType  = n.ReferenceType,
                referenceId    = n.ReferenceId,
            },
        },
        _ => null,
    };

    public (IEnumerable<string> UserIds, IEnumerable<string> GroupNames) GetTargets(object domainEvent)
        => Unwrap(domainEvent) switch
        {
            // Per-recipient: only the target user's connections. Never the admin group, never client-supplied targets.
            NotificationSentMessage n =>
                (Array.Empty<string>(), new[] { AdminNotificationHub.UserGroup(n.RecipientUserId) }),
            // Messaging/moderation: server-side single admin group (an admin observes all conversations).
            null => (Array.Empty<string>(), Array.Empty<string>()),
            _    => (Array.Empty<string>(), new[] { AdminMessagingHub.AdminGroup }),
        };

    // The generic RealtimeEventConsumer wraps the bus message in an EventDto (Data = the message); accept both the
    // wrapped and raw forms, and only our known message types (strict — an unrelated Data must not target anyone).
    private static object? Unwrap(object domainEvent) => domainEvent switch
    {
        MessagingMessageSentMessage                          => domainEvent,
        MessagingModerationEventMessage                      => domainEvent,
        NotificationSentMessage                              => domainEvent,
        EventDto { Data: MessagingMessageSentMessage d }     => d,
        EventDto { Data: MessagingModerationEventMessage d } => d,
        EventDto { Data: NotificationSentMessage d }         => d,
        _                                                    => null,
    };
}
