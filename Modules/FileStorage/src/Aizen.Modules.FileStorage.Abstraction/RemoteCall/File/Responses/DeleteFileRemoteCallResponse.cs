
namespace Aizen.Modules.FileStorage.Abstraction.RemoteCall.File.Responses;

[DocumentationInfo("Delete file remote call response", "Response model for the FileStorage DeleteFile remote call.")]
public sealed class DeleteFileRemoteCallResponse
{
    public Guid FileId { get; set; }
    public bool IsDeleted { get; set; }
}
