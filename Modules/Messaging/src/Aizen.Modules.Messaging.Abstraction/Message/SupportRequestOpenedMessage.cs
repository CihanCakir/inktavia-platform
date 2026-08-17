using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.Messaging.Abstraction.Enum;

namespace Aizen.Modules.Messaging.Abstraction.Message;

/// <summary>
/// N-D — published when a live-support request (a <see cref="MessagingContextType.Support"/> conversation) is opened.
/// Consumed by the Notification module to alert admins (SupportRequestOpened notification). Distinct from
/// MessagingMessageSentMessage because a brand-new support thread has no non-requester participant, so the ordinary
/// message-sent fan-out would produce no event.
/// </summary>
public sealed class SupportRequestOpenedMessage : AizenBaseMessage
{
    public long         ConversationId  { get; set; }
    public long         RequesterUserId { get; set; }
    public string       RequesterName   { get; set; } = default!;
    public SupportTopic Topic           { get; set; }
    public string       Subject         { get; set; } = default!;
}
