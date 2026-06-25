using Aizen.Core.Realtime.Abstraction.Interfaces;
using Aizen.Modules.Messaging.Abstraction.Dto;
using Aizen.Modules.Messaging.Abstraction.Enum;
using System.Text.Json;

namespace Aizen.Modules.Messaging.Application.Realtime;

[DocumentationInfo("Messaging realtime publisher",
    "Publishes typed messaging events to conversation groups and user channels via SignalR.")]
public sealed class MessagingRealtimePublisher
{
    private readonly IRealtimePublisher _publisher;
    public MessagingRealtimePublisher(IRealtimePublisher publisher) => _publisher = publisher;

    public async Task PublishMessageSentAsync(
        long conversationId,
        long contextId,
        MessagingContextType contextType,
        object messagePayload,
        IEnumerable<long> participantUserIds,
        CancellationToken ct = default)
    {
        var dto = new MessagingRealtimeEventDto
        {
            ConversationId = conversationId,
            ContextId      = contextId,
            ContextType    = contextType,
            EventType      = MessagingRealtimeEventType.MessageSent,
            PayloadJson    = JsonSerializer.Serialize(messagePayload),
            OccurredAt     = DateTimeOffset.UtcNow,
        };

        await _publisher.PublishToGroupAsync($"messaging:conv:{conversationId}", dto, ct: ct);

        foreach (var userId in participantUserIds)
            await _publisher.PublishToUserAsync(userId.ToString(), dto, ct: ct);
    }

    public async Task PublishModerationEventAsync(
        long conversationId,
        long messageId,
        string policyCode,
        string reason,
        CancellationToken ct = default)
    {
        var dto = new MessagingRealtimeEventDto
        {
            ConversationId = conversationId,
            EventType      = MessagingRealtimeEventType.MessageModerated,
            PayloadJson    = JsonSerializer.Serialize(new { messageId, policyCode, reason }),
            OccurredAt     = DateTimeOffset.UtcNow,
        };

        await _publisher.PublishToGroupAsync("admin:messaging-moderation", dto, ct: ct);
    }

    public async Task PublishConversationStatusChangedAsync(
        long conversationId,
        string newStatus,
        CancellationToken ct = default)
    {
        var dto = new MessagingRealtimeEventDto
        {
            ConversationId = conversationId,
            EventType      = MessagingRealtimeEventType.ConversationStatusChanged,
            PayloadJson    = JsonSerializer.Serialize(new { newStatus }),
            OccurredAt     = DateTimeOffset.UtcNow,
        };

        await _publisher.PublishToGroupAsync($"messaging:conv:{conversationId}", dto, ct: ct);
    }
}
