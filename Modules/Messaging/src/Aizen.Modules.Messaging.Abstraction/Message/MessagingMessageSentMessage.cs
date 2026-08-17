using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.Messaging.Abstraction.Enum;

namespace Aizen.Modules.Messaging.Abstraction.Message;

/// <summary>
/// Published by the Messaging module after a message is persisted.
/// Consumed by the Notification module to trigger new-message notifications.
/// </summary>
public sealed class MessagingMessageSentMessage : AizenBaseMessage
{
    public long                 ConversationId    { get; set; }
    public string               ConversationTitle { get; set; } = default!;
    public long                 SenderUserId      { get; set; }
    public string               SenderName        { get; set; } = default!;
    public MessagingContextType ContextType       { get; set; }
    public long                 ContextId         { get; set; }
    /// <summary>All participant UserIds EXCEPT the sender.</summary>
    public List<long>           RecipientUserIds  { get; set; } = [];
    public bool                 IsInternalNote    { get; set; }
    public DateTimeOffset       SentAt            { get; set; }
}
