using Aizen.Core.CQRS.Handler;
using Aizen.Modules.FileStorage.Domain.Interface.Repository;
using Aizen.Modules.FileStorage.Domain.Interface.Service;

namespace Aizen.Modules.FileStorage.Application.Commands.RejectFile;

[DocumentationInfo("Reject file command handler", "Resolves the file by Guid, then delegates to IFileStorageService to mark the file as rejected.")]
public sealed class RejectFileCommandHandler : AizenCommandHandler<RejectFileCommand, bool>
{
    private readonly IFileStorageService _storageService;
    private readonly IFileRepository _fileRepository;

    public RejectFileCommandHandler(IFileStorageService storageService, IFileRepository fileRepository)
    {
        _storageService = storageService;
        _fileRepository = fileRepository;
    }

    public override async Task<bool> Handle(RejectFileCommand command, CancellationToken cancellationToken)
    {
        var file = await _fileRepository.GetByGuidAsync(command.FileId, cancellationToken)
            ?? throw new KeyNotFoundException($"File not found: {command.FileId}");

        return await _storageService.RejectFileAsync(file.Id, cancellationToken);
    }
}
