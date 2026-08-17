using Aizen.Core.CQRS.Message;

namespace Aizen.Modules.Messaging.Application.Command.RequestAttachmentUploadUrl;

[DocumentationInfo("Request attachment upload URL command",
    "Creates a presigned S3 upload session for a conversation message attachment.")]
public sealed class RequestAttachmentUploadUrlCommand
    : AizenCommand<RequestAttachmentUploadUrlResponse>
{
    public long ConversationId { get; init; }
    public string FileName     { get; init; } = default!;
    public string ContentType  { get; init; } = default!;
    public long SizeInBytes    { get; init; }
}

public sealed record RequestAttachmentUploadUrlResponse(
    Guid FileId,
    string UploadSessionCode,
    string UploadUrl,
    DateTime ExpiresAt
);
