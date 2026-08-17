namespace Aizen.Modules.Messaging.Domain.Interface;

[DocumentationInfo("Messaging file storage service interface",
    "Abstraction for FileStorage integration — presigned upload URL generation and read URL resolution.")]
public interface IMessagingFileStorageService
{
    /// <summary>
    /// Creates a presigned S3 upload session via FileStorage message bus.
    /// Returns the upload URL, session code, and file ID for the client.
    /// </summary>
    Task<MessagingUploadSessionResult> CreateUploadSessionAsync(
        string fileName,
        string contentType,
        long sizeInBytes,
        long requestedByUserId,
        CancellationToken ct = default);

    /// <summary>
    /// Completes an upload session after the client uploads directly to S3.
    /// Returns the confirmed FileStorage Guid for the file.
    /// </summary>
    Task<Guid?> CompleteUploadSessionAsync(
        string uploadSessionCode,
        string? checksum,
        CancellationToken ct = default);

    /// <summary>
    /// Generates a time-limited presigned read URL for an existing file.
    /// Used when enriching message responses with attachment URLs.
    /// </summary>
    Task<string?> GetReadUrlAsync(
        Guid fileStorageId,
        TimeSpan? expiresIn = null,
        CancellationToken ct = default);
}

public sealed record MessagingUploadSessionResult(
    Guid FileId,
    string UploadSessionCode,
    string UploadUrl,
    DateTime ExpiresAt
);
