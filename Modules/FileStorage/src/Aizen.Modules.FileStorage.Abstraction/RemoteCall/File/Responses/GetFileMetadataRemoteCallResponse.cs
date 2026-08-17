using Aizen.Modules.FileStorage.Abstraction.Enum;

namespace Aizen.Modules.FileStorage.Abstraction.RemoteCall.File.Responses;

[DocumentationInfo("Get file metadata remote call response", "Response model for the FileStorage GetFileMetadata remote call.")]
public sealed class GetFileMetadataRemoteCallResponse
{
    public Guid FileId { get; set; }
    public string FileCode { get; set; } = default!;
    public string OriginalFileName { get; set; } = default!;
    public string ContentType { get; set; } = default!;
    public string Extension { get; set; } = default!;
    public long SizeInBytes { get; set; }
    public FileVisibility Visibility { get; set; }
    public FileStatus Status { get; set; }
    public DateTime? UploadedAt { get; set; }
}
