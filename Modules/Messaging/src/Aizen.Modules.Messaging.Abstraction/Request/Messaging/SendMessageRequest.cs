using Aizen.Modules.Messaging.Abstraction.Enum;

namespace Aizen.Modules.Messaging.Abstraction.Request.Messaging;

[DocumentationInfo("Send message request", "Payload for sending a message in a conversation.")]
public sealed record SendMessageRequest(
    string Content,
    MessageType Type = MessageType.Text,
    bool IsInternalNote = false,
    string? AttachmentFileStorageId = null,
    string? AttachmentFileName = null,
    string? AttachmentFileType = null
);
