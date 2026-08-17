using Aizen.Core.Domain;

namespace Aizen.Modules.Messaging.Domain.Entities.Conversation;

[DocumentationInfo("Message attachment entity",
    "A file attached to a conversation message. FileId resolves to a URL via FileStorage module.")]
public sealed class MessageAttachmentEntity : AizenEntityWithAudit
{
    public long MessageId         { get; private set; }
    public string FileName        { get; private set; } = string.Empty;
    public string FileType        { get; private set; } = "document";
    public string? FileStorageId  { get; private set; }

    public MessageAttachmentEntity() { }

    public void SetFileStorageId(string fileStorageId)
        => FileStorageId = fileStorageId;

    public static MessageAttachmentEntity Create(
        long messageId,
        string fileName,
        string fileType,
        string? fileStorageId = null)
    {
        return new MessageAttachmentEntity
        {
            MessageId     = messageId,
            FileName      = fileName.Trim(),
            FileType      = fileType.ToLower(),
            FileStorageId = fileStorageId,
            IsActive      = true,
        };
    }
}
