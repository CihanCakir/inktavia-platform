using Aizen.Core.CQRS.Message;
using Aizen.Modules.FileStorage.Abstraction.Dto.File;
using Aizen.Modules.FileStorage.Abstraction.Enum;

namespace Aizen.Modules.FileStorage.Application.Commands.UpdateFileVisibility;

[DocumentationInfo("Update file visibility command", "Changes the visibility (public/private) of a file.")]
public sealed class UpdateFileVisibilityCommand : AizenCommand<FileDto>
{
    public long FileId { get; set; }
    public FileVisibility Visibility { get; set; }
}
