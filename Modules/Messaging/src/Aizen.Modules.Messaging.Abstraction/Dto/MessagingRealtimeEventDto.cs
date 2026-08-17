using Aizen.Modules.Messaging.Abstraction.Enum;

namespace Aizen.Modules.Messaging.Abstraction.Dto;

[DocumentationInfo("Messaging realtime event DTO",
    "Payload published via SignalR for all messaging events.")]
public sealed class MessagingRealtimeEventDto
{
    public long ConversationId                   { get; set; }
    public long ContextId                        { get; set; }
    public MessagingContextType ContextType      { get; set; }
    public MessagingRealtimeEventType EventType  { get; set; }
    public string? PayloadJson                   { get; set; }
    public DateTimeOffset OccurredAt             { get; set; }
}
