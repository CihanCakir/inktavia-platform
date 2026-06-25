using Aizen.Core.CQRS.Message;
using Aizen.Modules.Messaging.Abstraction.Enum;
using Aizen.Modules.Messaging.Abstraction.Response.Messaging;

namespace Aizen.Modules.Messaging.Application.Command.CreateConversation;

[DocumentationInfo("Create conversation command", "Carries payload to create or retrieve an existing conversation by context.")]
public sealed class CreateConversationCommand : AizenCommand<CreateConversationResponse>
{
    public MessagingContextType ContextType                { get; }
    public long ContextId                                  { get; }
    public string Title                                    { get; }
    public List<ConversationParticipantInput> Participants { get; }

    public CreateConversationCommand(
        MessagingContextType contextType,
        long contextId,
        string title,
        List<ConversationParticipantInput> participants)
    {
        ContextType  = contextType;
        ContextId    = contextId;
        Title        = title;
        Participants = participants;
    }
}

public sealed record ConversationParticipantInput(
    long UserId,
    string DisplayName,
    MessagingParticipantRole Role);
