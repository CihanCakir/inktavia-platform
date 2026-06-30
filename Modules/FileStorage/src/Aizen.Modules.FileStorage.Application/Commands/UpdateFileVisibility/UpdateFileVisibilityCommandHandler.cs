using Aizen.Core.CQRS.Handler;
using Aizen.Modules.FileStorage.Abstraction.Dto.File;
using Aizen.Modules.FileStorage.Domain.Interface.Service;

namespace Aizen.Modules.FileStorage.Application.Commands.UpdateFileVisibility;

[DocumentationInfo("Update file visibility command handler", "Delegates to IFileStorageService to update file visibility.")]
public sealed class UpdateFileVisibilityCommandHandler : AizenCommandHandler<UpdateFileVisibilityCommand, FileDto>
{
    private readonly IFileStorageService _storageService;

    public UpdateFileVisibilityCommandHandler(IFileStorageService storageService)
    {
        _storageService = storageService;
    }

    public override async Task<FileDto?> Handle(UpdateFileVisibilityCommand command, CancellationToken cancellationToken)
    {
        return await _storageService.UpdateVisibilityAsync(command.FileId, command.Visibility, cancellationToken);
    }
}
