using Aizen.Core.CQRS.Message;
using Aizen.Modules.FileStorage.Abstraction.Request.File;

namespace Aizen.Modules.FileStorage.Application.Commands.DeleteFile;

[DocumentationInfo("Delete file command", "Soft-deletes a file from the system.")]
public sealed class DeleteFileCommand : AizenCommand<bool>
{
    public long FileId { get; set; }
    public DeleteFileRequest Request { get; set; } = default!;
}
