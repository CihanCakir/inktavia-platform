using Aizen.Core.Domain;
using Aizen.Modules.Messaging.Abstraction.Enum;

namespace Aizen.Modules.Messaging.Domain.Entities.Conversation;

[DocumentationInfo("Conversation message entity",
    "A single message in a conversation. Carries moderation status and optional attachment.")]
public sealed class ConversationMessageEntity : AizenEntityWithAudit
{
    public long ConversationId                       { get; private set; }
    public long SenderUserId                         { get; private set; }
    public string SenderName                         { get; private set; } = string.Empty;
    public MessagingParticipantRole SenderRole       { get; private set; }
    public string Content                            { get; private set; } = string.Empty;
    public MessageType Type                          { get; private set; } = MessageType.Text;
    public bool IsInternalNote                       { get; private set; }
    public MessageModerationStatus ModerationStatus  { get; private set; } = MessageModerationStatus.Allowed;
    public string? ModerationReason                  { get; private set; }
    public DateTimeOffset SentAt                     { get; private set; }

    private readonly List<MessageAttachmentEntity> _attachments = new();
    public IReadOnlyCollection<MessageAttachmentEntity> Attachments => _attachments.AsReadOnly();

    public ConversationMessageEntity() { }

    public static ConversationMessageEntity Create(
        long conversationId,
        long senderUserId,
        string senderName,
        MessagingParticipantRole senderRole,
        string content,
        MessageType type = MessageType.Text,
        bool isInternalNote = false)
    {
        return new ConversationMessageEntity
        {
            ConversationId   = conversationId,
            SenderUserId     = senderUserId,
            SenderName       = senderName.Trim(),
            SenderRole       = senderRole,
            Content          = content.Trim(),
            Type             = type,
            IsInternalNote   = isInternalNote,
            ModerationStatus = MessageModerationStatus.Allowed,
            SentAt           = DateTimeOffset.UtcNow,
            IsActive         = true,
        };
    }

    public void AddAttachment(MessageAttachmentEntity attachment)
        => _attachments.Add(attachment);

    public void SetModerationStatus(MessageModerationStatus status, string? reason = null)
    {
        ModerationStatus = status;
        ModerationReason = reason;
    }

    public void Flag(string reason)
        => SetModerationStatus(MessageModerationStatus.Flagged, reason);

    public void Block(string reason)
        => SetModerationStatus(MessageModerationStatus.Blocked, reason);
}
