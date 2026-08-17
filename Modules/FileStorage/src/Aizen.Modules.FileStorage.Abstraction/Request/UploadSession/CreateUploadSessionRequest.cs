using Aizen.Modules.FileStorage.Abstraction.Enum;

namespace Aizen.Modules.FileStorage.Abstraction.Request.UploadSession;

[DocumentationInfo("Create upload session request", "Parameters required to create a new S3 pre-signed upload session.")]
public sealed class CreateUploadSessionRequest
{
    public string OriginalFileName { get; set; } = default!;
    public string ContentType { get; set; } = default!;
    public long SizeInBytes { get; set; }
    public FileCategory Category { get; set; }
    public FileVisibility Visibility { get; set; } = FileVisibility.Private;
    public string? OwnerModule { get; set; }
    public string? OwnerEntityType { get; set; }
    public Guid? OwnerEntityId { get; set; }
    /// <summary>
    /// When true, the presigned upload URL is signed with the internal S3 endpoint (ServiceUrl, e.g. http://minio:9000)
    /// instead of the browser-facing PublicServiceUrl. Use for server-to-server uploads where the browser is never involved.
    /// </summary>
    public bool ServerSideUpload { get; set; }
}
