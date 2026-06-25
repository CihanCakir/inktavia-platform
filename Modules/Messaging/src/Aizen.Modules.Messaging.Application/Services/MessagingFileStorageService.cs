using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.FileStorage.Abstraction.Enum;
using Aizen.Modules.FileStorage.Abstraction.Message;
using Aizen.Modules.Messaging.Domain.Interface;

namespace Aizen.Modules.Messaging.Application.Services;

[DocumentationInfo("Messaging file storage service",
    "Integrates with FileStorage module via MassTransit message bus for presigned upload/read URLs.")]
public sealed class MessagingFileStorageService : IMessagingFileStorageService
{
    private readonly IAizenMessagePublisher _publisher;

    public MessagingFileStorageService(IAizenMessagePublisher publisher)
    {
        _publisher = publisher;
    }

    public async Task<MessagingUploadSessionResult> CreateUploadSessionAsync(
        string fileName, string contentType, long sizeInBytes,
        long requestedByUserId, CancellationToken ct = default)
    {
        var category = ResolveCategory(contentType);

        var result = await _publisher.SendAsync<
            CreateUploadSessionProcessMessage,
            CreateUploadSessionProcessMessageResult>(
            new CreateUploadSessionProcessMessage
            {
                OriginalFileName  = fileName,
                ContentType       = contentType,
                SizeInBytes       = sizeInBytes,
                Category          = category,
                Visibility        = FileVisibility.Private,
                OwnerModule       = "Messaging",
                OwnerEntityType   = "ConversationMessage",
                OwnerEntityId     = null,
                RequestedByUserId = requestedByUserId
            }, ct);

        return new MessagingUploadSessionResult(
            result.FileId,
            result.UploadSessionCode,
            result.UploadUrl,
            result.ExpiresAt);
    }

    public async Task<Guid?> CompleteUploadSessionAsync(
        string uploadSessionCode, string? checksum, CancellationToken ct = default)
    {
        var result = await _publisher.SendAsync<
            CompleteUploadSessionProcessMessage,
            CompleteUploadSessionProcessMessageResult>(
            new CompleteUploadSessionProcessMessage
            {
                UploadSessionCode = uploadSessionCode,
                Checksum          = checksum
            }, ct);

        return result.Status != FileStatus.Rejected ? result.FileId : null;
    }

    public async Task<string?> GetReadUrlAsync(
        Guid fileStorageId, TimeSpan? expiresIn = null, CancellationToken ct = default)
    {
        var result = await _publisher.SendAsync<
            CreateFileReadUrlProcessMessage,
            CreateFileReadUrlProcessMessageResult>(
            new CreateFileReadUrlProcessMessage
            {
                FileId    = fileStorageId,
                ExpiresIn = expiresIn ?? TimeSpan.FromMinutes(30)
            }, ct);

        return string.IsNullOrWhiteSpace(result.ReadUrl) ? null : result.ReadUrl;
    }

    private static FileCategory ResolveCategory(string contentType) => contentType switch
    {
        var ct when ct.StartsWith("image/") => FileCategory.Image,
        var ct when ct.StartsWith("video/") => FileCategory.Video,
        "application/pdf"                   => FileCategory.Document,
        _                                   => FileCategory.Other
    };
}
