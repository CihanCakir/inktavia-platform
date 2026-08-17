using Aizen.Core.Domain;
using Aizen.Modules.Messaging.Abstraction.Enum;

namespace Aizen.Modules.Messaging.Domain.Entities.Conversation;

[DocumentationInfo("Conversation participant entity",
    "A user who is a member of a conversation thread. DisplayName is captured at join time.")]
public sealed class ConversationParticipantEntity : AizenEntityWithAudit
{
    public long ConversationId           { get; private set; }
    public long UserId                   { get; private set; }
    public string DisplayName            { get; private set; } = string.Empty;
    public MessagingParticipantRole Role { get; private set; }
    public DateTimeOffset JoinedAt       { get; private set; }

    public ConversationParticipantEntity() { }

    public static ConversationParticipantEntity Create(
        long conversationId,
        long userId,
        string displayName,
        MessagingParticipantRole role)
    {
        return new ConversationParticipantEntity
        {
            ConversationId = conversationId,
            UserId         = userId,
            DisplayName    = displayName.Trim(),
            Role           = role,
            JoinedAt       = DateTimeOffset.UtcNow,
            IsActive       = true,
        };
    }

    public void Leave() => IsActive = false;
}
