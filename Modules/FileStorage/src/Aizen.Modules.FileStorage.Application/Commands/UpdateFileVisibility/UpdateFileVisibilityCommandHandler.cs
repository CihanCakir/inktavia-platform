using Aizen.Core.CQRS.Handler;
using Aizen.Modules.FileStorage.Abstraction.Dto.File;
using Aizen.Modules.FileStorage.Domain.Interface.Repository;
using Aizen.Modules.FileStorage.Domain.Interface.Service;

namespace Aizen.Modules.FileStorage.Application.Commands.UpdateFileVisibility;

[DocumentationInfo("Update file visibility command handler", "Resolves the file by Guid, then delegates to IFileStorageService to update file visibility.")]
public sealed class UpdateFileVisibilityCommandHandler : AizenCommandHandler<UpdateFileVisibilityCommand, FileDto>
{
    private readonly IFileStorageService _storageService;
    private readonly IFileRepository _fileRepository;

    public UpdateFileVisibilityCommandHandler(IFileStorageService storageService, IFileRepository fileRepository)
    {
        _storageService = storageService;
        _fileRepository = fileRepository;
    }

    public override async Task<FileDto?> Handle(UpdateFileVisibilityCommand command, CancellationToken cancellationToken)
    {
        var file = await _fileRepository.GetByGuidAsync(command.FileId, cancellationToken)
            ?? throw new KeyNotFoundException($"File not found: {command.FileId}");

        return await _storageService.UpdateVisibilityAsync(file.Id, command.Visibility, cancellationToken);
    }
}
