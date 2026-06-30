
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
    public int ReadUrlExpirationMinutes { get; set; } = 60;
}
