using Aizen.Core.Domain;
using Aizen.Modules.Messaging.Abstraction.Enum;

namespace Aizen.Modules.Messaging.Domain.Entities.Conversation;

[DocumentationInfo("Conversation entity",
    "Root aggregate for a messaging thread. Context-agnostic: owned by any domain via ContextType+ContextId.")]
public sealed class ConversationEntity : AizenEntityWithAudit
{
    public MessagingContextType ContextType  { get; private set; }
    public long ContextId                    { get; private set; }
    public string Title                      { get; private set; } = string.Empty;
    public ConversationStatus Status         { get; private set; } = ConversationStatus.Active;
    public int UnreadCountByAdmin            { get; private set; }
    public DateTimeOffset LastMessageAt      { get; private set; }
    public string LastMessagePreview         { get; private set; } = string.Empty;
    /// <summary>N-D support reason/topic — set for <see cref="MessagingContextType.Support"/> conversations; null otherwise.</summary>
    public SupportTopic? Topic               { get; private set; }

    private readonly List<ConversationParticipantEntity> _participants = new();
    public IReadOnlyCollection<ConversationParticipantEntity> Participants => _participants.AsReadOnly();

    private readonly List<ConversationMessageEntity> _messages = new();
    public IReadOnlyCollection<ConversationMessageEntity> Messages => _messages.AsReadOnly();

    public ConversationEntity() { }

    public static ConversationEntity Create(
        MessagingContextType contextType,
        long contextId,
        string title,
        SupportTopic? topic = null)
    {
        return new ConversationEntity
        {
            ContextType        = contextType,
            ContextId          = contextId,
            Title              = title.Trim(),
            Topic              = topic,
            Status             = ConversationStatus.Active,
            UnreadCountByAdmin = 0,
            LastMessageAt      = DateTimeOffset.UtcNow,
            LastMessagePreview = string.Empty,
            IsActive           = true,
        };
    }

    public void AddParticipant(ConversationParticipantEntity participant)
        => _participants.Add(participant);

    public void AddMessage(ConversationMessageEntity message)
    {
        _messages.Add(message);
        LastMessageAt      = message.SentAt;
        LastMessagePreview = message.Content.Length > 120
            ? message.Content[..120] + "…"
            : message.Content;
        if (!message.IsInternalNote)
            UnreadCountByAdmin++;
    }

    public void MarkReadByAdmin()
        => UnreadCountByAdmin = 0;

    public void SetStatus(ConversationStatus status)
        => Status = status;

    public void Flag()
        => Status = ConversationStatus.Flagged;

    public void Archive()
        => Status = ConversationStatus.Archived;
}
