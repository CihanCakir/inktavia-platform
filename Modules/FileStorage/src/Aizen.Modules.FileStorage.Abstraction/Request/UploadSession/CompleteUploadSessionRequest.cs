using Aizen.Modules.FileStorage.Abstraction.Model;

namespace Aizen.Modules.FileStorage.Abstraction.Request.UploadSession;

[DocumentationInfo("Complete upload session request", "Finalizes an upload session after client has uploaded file to S3.")]
public sealed class CompleteUploadSessionRequest
{
    public string? Checksum { get; set; }
}
