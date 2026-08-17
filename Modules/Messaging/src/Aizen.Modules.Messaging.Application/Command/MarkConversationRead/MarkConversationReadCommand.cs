using Aizen.Core.CQRS.Message;

namespace Aizen.Modules.Messaging.Application.Command.MarkConversationRead;

[DocumentationInfo("Mark conversation read command", "Resets the admin unread message counter for a conversation.")]
public sealed class MarkConversationReadCommand : AizenCommand<bool>
{
    public long ConversationId { get; }
    public MarkConversationReadCommand(long conversationId) => ConversationId = conversationId;
}
