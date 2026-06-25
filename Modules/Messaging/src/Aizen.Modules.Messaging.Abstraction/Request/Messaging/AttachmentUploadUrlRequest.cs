namespace Aizen.Modules.Messaging.Abstraction.Request.Messaging;

[DocumentationInfo("Attachment upload URL request",
    "Payload for requesting a presigned S3 upload URL for a conversation message attachment.")]
public sealed record AttachmentUploadUrlRequest(
    string FileName,
    string ContentType,
    long SizeInBytes
);
