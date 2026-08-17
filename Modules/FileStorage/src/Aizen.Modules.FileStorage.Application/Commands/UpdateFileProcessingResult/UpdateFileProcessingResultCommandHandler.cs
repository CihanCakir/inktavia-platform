using Aizen.Core.CQRS.Handler;
using Aizen.Modules.FileStorage.Domain.Interface.Repository;
using Aizen.Modules.FileStorage.Domain.Interface.Service;

namespace Aizen.Modules.FileStorage.Application.Commands.UpdateFileProcessingResult;

[DocumentationInfo("Update file processing result command handler", "Resolves the file by Guid, then delegates to IFileProcessingService to record the job outcome.")]
public sealed class UpdateFileProcessingResultCommandHandler : AizenCommandHandler<UpdateFileProcessingResultCommand, bool>
{
    private readonly IFileProcessingService _processingService;
    private readonly IFileRepository _fileRepository;

    public UpdateFileProcessingResultCommandHandler(IFileProcessingService processingService, IFileRepository fileRepository)
    {
        _processingService = processingService;
        _fileRepository = fileRepository;
    }

    public override async Task<bool> Handle(UpdateFileProcessingResultCommand command, CancellationToken cancellationToken)
    {
        var file = await _fileRepository.GetByGuidAsync(command.FileId, cancellationToken)
            ?? throw new KeyNotFoundException($"File not found: {command.FileId}");

        return await _processingService.UpdateResultAsync(file.Id, command.Request, cancellationToken);
    }
}
