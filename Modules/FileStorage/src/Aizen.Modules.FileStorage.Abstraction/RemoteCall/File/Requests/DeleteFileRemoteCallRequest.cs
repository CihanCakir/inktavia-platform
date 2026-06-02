using Aizen.Modules.FileStorage.Abstraction.Enum;
using Aizen.Modules.FileStorage.Abstraction.Model;

namespace Aizen.Modules.FileStorage.Abstraction.RemoteCall.File.Requests;

[DocumentationInfo("Delete file remote call request", "Request model for the FileStorage DeleteFile remote call.")]
public sealed class DeleteFileRemoteCallRequest
{
    public FileDeleteBehavior DeleteBehavior { get; set; } = FileDeleteBehavior.SoftDeleteOnly;
}
