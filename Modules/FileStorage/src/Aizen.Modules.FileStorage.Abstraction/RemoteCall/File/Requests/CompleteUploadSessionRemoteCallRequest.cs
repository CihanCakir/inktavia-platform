
namespace Aizen.Modules.FileStorage.Abstraction.RemoteCall.File.Requests;

[DocumentationInfo("Complete upload session remote call request", "Request model for the FileStorage CompleteUploadSession remote call.")]
public sealed class CompleteUploadSessionRemoteCallRequest
{
    public string? Checksum { get; set; }
}
