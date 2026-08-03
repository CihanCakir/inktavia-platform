using Aizen.Core.Realtime.Abstraction.Interfaces;
using Aizen.Core.Realtime.Abstraction.Models;
using Aizen.Modules.Messaging.Abstraction.Message;

namespace Aizen.Bff.AdminPanel.Realtime;

/// <summary>
/// The single per-surface routing declaration (ADR layer-2): "this module bus event → this thin frame + these
/// target groups". The framework's <c>RealtimeIngressService</c> resolves one <see cref="IEventSocketMapper"/>
/// and calls <see cref="Map"/> + <see cref="GetTargets"/> for each consumed event.
///
/// Handles two module bus events, both to the single admin group as a THIN "messagingEvent" frame (no content —
/// the SPA refetches over HTTP), discriminated by the inner payload <c>type</c>:
///   • <see cref="MessagingMessageSentMessage"/>       → { type = "MessageAdded",     conversationId }
///   • <see cref="MessagingModerationEventMessage"/>   → { type = "ModerationEvent",  conversationId, messageId, kind, newStatus }
/// The framework resolves ONE <see cref="IEventSocketMapper"/>, so both types are broadened into this single mapper
/// rather than registering a second one (ADR layer-2).
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
        _ => null,
    };

    public (IEnumerable<string> UserIds, IEnumerable<string> GroupNames) GetTargets(object domainEvent)
    {
        if (Unwrap(domainEvent) is null)
            return (Array.Empty<string>(), Array.Empty<string>());

        // Server-side group policy: broadcast to the single admin group only. No client-supplied targets.
        return (Array.Empty<string>(), new[] { AdminMessagingHub.AdminGroup });
    }

    // The generic RealtimeEventConsumer wraps the bus message in an EventDto (Data = the message); accept both the
    // wrapped and raw forms, and only our two message types (strict — an unrelated Data must not target admins).
    private static object? Unwrap(object domainEvent) => domainEvent switch
    {
        MessagingMessageSentMessage                        => domainEvent,
        MessagingModerationEventMessage                    => domainEvent,
        EventDto { Data: MessagingMessageSentMessage d }     => d,
        EventDto { Data: MessagingModerationEventMessage d } => d,
        _                                                  => null,
    };
}
