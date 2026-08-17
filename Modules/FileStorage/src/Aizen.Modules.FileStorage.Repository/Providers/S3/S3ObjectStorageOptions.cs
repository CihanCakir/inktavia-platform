
namespace Aizen.Modules.FileStorage.Repository.Providers.S3;

[DocumentationInfo("S3 object storage options", "Configuration options for S3-compatible object storage. Supports AWS S3 and MinIO.")]
public sealed class S3ObjectStorageOptions
{
    /// <summary>"AWS" or "MinIO"</summary>
    public string Provider { get; set; } = "AWS";
    public string AccessKey { get; set; } = default!;
    public string SecretKey { get; set; } = default!;
    public string Region { get; set; } = default!;
    public string BucketName { get; set; } = default!;
    /// <summary>Required for MinIO or custom S3-compatible endpoints. Null for AWS S3.</summary>
    public string? ServiceUrl { get; set; }
    /// <summary>Must be true for MinIO. Must be false for AWS S3.</summary>
    public bool ForcePathStyle { get; set; }
    /// <summary>Use HTTP instead of HTTPS. Only true for local MinIO.</summary>
    public bool UseHttp { get; set; }
    public int UploadUrlExpirationMinutes { get; set; } = 15;
    public int ReadUrlExpirationMinutes { get; set; } = 5;
    /// <summary>
    /// Browser-reachable endpoint used ONLY when signing presigned URLs (upload/read). The browser cannot resolve
    /// the internal container hostname, and SigV4 signs the Host header, so the URL must be signed against the host
    /// the client will actually call. Falls back to ServiceUrl when null (e.g. real AWS S3, where both are the same).
    /// </summary>
    public string? PublicServiceUrl { get; set; }
}
