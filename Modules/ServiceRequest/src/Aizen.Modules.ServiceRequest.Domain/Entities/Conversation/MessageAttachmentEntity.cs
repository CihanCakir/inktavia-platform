using Aizen.Core.Domain;

namespace Aizen.Modules.ServiceRequest.Domain.Entities.Conversation;

[DocumentationInfo("Message attachment entity", "A file attachment on a conversation message.")]
public sealed class MessageAttachmentEntity : AizenEntityWithAudit
{
    public long MessageId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Type { get; private set; } = "document";
    public string? FileId { get; private set; }

    public MessageAttachmentEntity() { }

    public static MessageAttachmentEntity Create(long messageId, string name, string type, string? fileId)
    {
        return new MessageAttachmentEntity
        {
            MessageId = messageId,
            Name = name.Trim(),
            Type = type,
            FileId = fileId,
            IsActive = true
        };
    }
}
