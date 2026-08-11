using Aizen.Modules.Messaging.Abstraction.Enum;

namespace Aizen.Modules.Messaging.Abstraction.Request.Messaging;

[DocumentationInfo("Send message request", "Payload for sending a message in a conversation.")]
public sealed record SendMessageRequest(
    string Content,
    MessageType Type = MessageType.Text,
    bool IsInternalNote = false,
    string? AttachmentFileStorageId = null,
    string? AttachmentFileName = null,
    string? AttachmentFileType = null,
    string? UploadSessionCode = null,
    string? Checksum = null,
    string? LocationJson = null,
    // BE_WC2 — discrete geo payload persisted to the WC0 columns when Type == Location (parity with the SR write path).
    decimal? LocationLat = null,
    decimal? LocationLng = null,
    string? LocationLabel = null
);
