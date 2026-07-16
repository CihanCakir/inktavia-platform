
namespace Aizen.Modules.FileStorage.Domain.Interface.Service;

[DocumentationInfo("Object storage provider interface", "Abstracts over S3/MinIO/local object storage for upload URL generation, read URL generation and object management.")]
public interface IObjectStorageProvider
{
    Task<string> GenerateUploadUrlAsync(string bucketName, string objectKey, string contentType, TimeSpan expiresIn, CancellationToken cancellationToken = default);
    Task<string> GenerateReadUrlAsync(string bucketName, string objectKey, TimeSpan expiresIn, CancellationToken cancellationToken = default);
    Task<bool> ObjectExistsAsync(string bucketName, string objectKey, CancellationToken cancellationToken = default);
    Task<ObjectMetadataResult> GetObjectMetadataAsync(string bucketName, string objectKey, CancellationToken cancellationToken = default);
    Task DeleteObjectAsync(string bucketName, string objectKey, CancellationToken cancellationToken = default);
    Task<byte[]> ReadFirstBytesAsync(string bucketName, string objectKey, int byteCount, CancellationToken cancellationToken = default);
    Task PutObjectAsync(string bucketName, string objectKey, byte[] content, string contentType, CancellationToken cancellationToken = default);
}

[DocumentationInfo("Object metadata result", "Holds metadata returned by the object storage provider for an existing object.")]
public sealed class ObjectMetadataResult
{
    public long ContentLength { get; init; }
    public string ContentType { get; init; } = default!;
    public string? ETag { get; init; }
    public DateTime LastModified { get; init; }
}
