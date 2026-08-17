using Aizen.Modules.FileStorage.Abstraction.Enum;

namespace Aizen.Modules.FileStorage.Abstraction.RemoteCall.File.Responses;

[DocumentationInfo("Complete upload session remote call response", "Response model for the FileStorage CompleteUploadSession remote call.")]
public sealed class CompleteUploadSessionRemoteCallResponse
{
    public Guid FileId { get; set; }
    public FileStatus Status { get; set; }
}
