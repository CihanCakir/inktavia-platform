using Aizen.Core.CQRS.Message;

namespace Aizen.Modules.Messaging.Application.Command.FlagConversation;

[DocumentationInfo("Flag conversation command", "Flags a conversation for moderation review.")]
public sealed class FlagConversationCommand : AizenCommand<bool>
{
    public long ConversationId { get; }
    public string Reason       { get; }

    public FlagConversationCommand(long conversationId, string reason)
    {
        ConversationId = conversationId;
        Reason         = reason;
    }
}
