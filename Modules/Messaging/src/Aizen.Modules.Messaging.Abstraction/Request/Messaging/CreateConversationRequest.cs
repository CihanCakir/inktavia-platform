using Aizen.Modules.Messaging.Abstraction.Enum;

namespace Aizen.Modules.Messaging.Abstraction.Request.Messaging;

[DocumentationInfo("Create conversation request", "Payload for creating a new conversation.")]
public sealed record CreateConversationRequest(
    MessagingContextType ContextType,
    long ContextId,
    string Title,
    List<CreateConversationParticipantInput> Participants
);

public sealed record CreateConversationParticipantInput(
    long UserId,
    string DisplayName,
    MessagingParticipantRole Role
);
