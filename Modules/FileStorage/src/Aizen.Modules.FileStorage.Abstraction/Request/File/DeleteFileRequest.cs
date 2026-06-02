using Aizen.Modules.FileStorage.Abstraction.Enum;
using Aizen.Modules.FileStorage.Abstraction.Model;

namespace Aizen.Modules.FileStorage.Abstraction.Request.File;

[DocumentationInfo("Delete file request", "Instructs the FileStorage module to delete a file.")]
public sealed class DeleteFileRequest
{
    public FileDeleteBehavior DeleteBehavior { get; set; } = FileDeleteBehavior.SoftDeleteOnly;
}
