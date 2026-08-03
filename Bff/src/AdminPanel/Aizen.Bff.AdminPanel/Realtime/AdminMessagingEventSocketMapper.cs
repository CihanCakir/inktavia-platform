using Aizen.Core.Realtime.Abstraction.Interfaces;
using Aizen.Core.Realtime.Abstraction.Models;
using Aizen.Modules.Messaging.Abstraction.Message;

namespace Aizen.Bff.AdminPanel.Realtime;

/// <summary>
/// The single per-surface routing declaration (ADR layer-2): "this module bus event → this thin frame + these
/// target groups". The framework's <c>RealtimeIngressService</c> resolves one <see cref="IEventSocketMapper"/>
/// and calls <see cref="Map"/> + <see cref="GetTargets"/> for each consumed event.
///
/// Wave 1 handles only <see cref="MessagingMessageSentMessage"/> (the module publishes it for non-internal-note
/// messages aimed at participants — exactly the user↔provider traffic an admin observes). The frame is
/// intentionally THIN: event name "messagingEvent" + { type, conversationId } and NO message content — the SPA
/// refetches over HTTP. Internal-note/flag/status live events are a later wave (add thin admin bus events then).
/// </summary>
public sealed class AdminMessagingEventSocketMapper : IEventSocketMapper
{
    public RealtimeMessage? Map(object domainEvent)
    {
        if (Extract(domainEvent) is not { } msg) return null;

        return new RealtimeMessage
        {
            Type       = "messagingEvent",
            Stream     = AdminMessagingHub.AdminGroup,
            AggregateId = msg.ConversationId.ToString(),
            Payload    = new { type = "MessageAdded", conversationId = msg.ConversationId },
        };
    }

    public (IEnumerable<string> UserIds, IEnumerable<string> GroupNames) GetTargets(object domainEvent)
    {
        if (Extract(domainEvent) is null)
            return (Array.Empty<string>(), Array.Empty<string>());

        // Server-side group policy: broadcast to the single admin group only. No client-supplied targets.
        return (Array.Empty<string>(), new[] { AdminMessagingHub.AdminGroup });
    }

    // The generic RealtimeEventConsumer wraps the bus message in an EventDto (Data = the message); accept both
    // the wrapped and raw forms so the mapper is robust to how it's invoked.
    private static MessagingMessageSentMessage? Extract(object domainEvent) => domainEvent switch
    {
        MessagingMessageSentMessage m                   => m,
        EventDto { Data: MessagingMessageSentMessage m } => m,
        _                                                => null,
    };
}
