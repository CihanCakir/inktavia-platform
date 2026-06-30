using Aizen.Core.CQRS.Handler;
using Aizen.Modules.FileStorage.Domain.Interface.Service;

namespace Aizen.Modules.FileStorage.Application.Commands.RejectFile;

[DocumentationInfo("Reject file command handler", "Delegates to IFileStorageService to mark the file as rejected.")]
public sealed class RejectFileCommandHandler : AizenCommandHandler<RejectFileCommand, bool>
{
    private readonly IFileStorageService _storageService;

    public RejectFileCommandHandler(IFileStorageService storageService)
    {
        _storageService = storageService;
    }

    public override async Task<bool> Handle(RejectFileCommand command, CancellationToken cancellationToken)
    {
        return await _storageService.RejectFileAsync(command.FileId, cancellationToken);
    }
}
