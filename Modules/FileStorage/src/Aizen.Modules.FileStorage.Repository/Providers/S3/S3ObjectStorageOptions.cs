using Aizen.Modules.FileStorage.Abstraction.Model;

namespace Aizen.Modules.FileStorage.Repository.Providers.S3;

[DocumentationInfo("S3 object storage options", "Configuration options for AWS S3 object storage provider.")]
public sealed class S3ObjectStorageOptions
{
    public string AccessKey { get; set; } = default!;
    public string SecretKey { get; set; } = default!;
    public string Region { get; set; } = default!;
    public string BucketName { get; set; } = default!;
    public int UploadUrlExpirationMinutes { get; set; } = 15;
    public int ReadUrlExpirationMinutes { get; set; } = 60;
}
