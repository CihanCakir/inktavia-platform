using Aizen.Core.CQRS.Handler;
using Aizen.Modules.FileStorage.Abstraction.Model;
using Aizen.Modules.FileStorage.Domain.Interface.Service;

namespace Aizen.Modules.FileStorage.Application.Commands.UpdateFileProcessingResult;

[DocumentationInfo("Update file processing result command handler", "Delegates to IFileProcessingService to record the job outcome.")]
public sealed class UpdateFileProcessingResultCommandHandler : AizenCommandHandler<UpdateFileProcessingResultCommand, bool>
{
    private readonly IFileProcessingService _processingService;

    public UpdateFileProcessingResultCommandHandler(IFileProcessingService processingService)
    {
        _processingService = processingService;
    }

    public override async Task<bool> Handle(UpdateFileProcessingResultCommand command, CancellationToken cancellationToken)
    {
        return await _processingService.UpdateResultAsync(command.FileId, command.Request, cancellationToken);
    }
}
