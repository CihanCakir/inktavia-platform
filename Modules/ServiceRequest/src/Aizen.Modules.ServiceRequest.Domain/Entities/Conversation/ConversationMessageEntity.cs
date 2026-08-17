using Aizen.Core.Domain;

namespace Aizen.Modules.ServiceRequest.Domain.Entities.Conversation;

[DocumentationInfo("Conversation message entity", "A message sent in a service request conversation thread.")]
public sealed class ConversationMessageEntity : AizenEntityWithAudit
{
    public long ConversationId { get; private set; }
    public string SenderId { get; private set; } = string.Empty;
    public string SenderName { get; private set; } = string.Empty;
    public string SenderRole { get; private set; } = "Owner";
    public string Content { get; private set; } = string.Empty;
    public bool IsInternalNote { get; private set; }
    public DateTimeOffset SentAt { get; private set; }

    private readonly List<MessageAttachmentEntity> _attachments = new();
    public IReadOnlyCollection<MessageAttachmentEntity> Attachments => _attachments.AsReadOnly();

    public ConversationMessageEntity() { }

    public static ConversationMessageEntity Create(
        long conversationId, string senderId, string senderName,
        string senderRole, string content, bool isInternalNote)
    {
        return new ConversationMessageEntity
        {
            ConversationId = conversationId,
            SenderId = senderId,
            SenderName = senderName,
            SenderRole = senderRole,
            Content = content.Trim(),
            IsInternalNote = isInternalNote,
            SentAt = DateTimeOffset.UtcNow,
            IsActive = true
        };
    }

    public void AddAttachment(MessageAttachmentEntity attachment) => _attachments.Add(attachment);
}
