using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminFiles.Command;

[DocumentationInfo("UpdateFileVisibility command handler", "Updates the visibility (public/private) of a file via the FileStorage module.")]
public sealed class UpdateFileVisibilityCommandHandler : AizenCommandHandler<UpdateFileVisibilityCommand, EmptyResult>
{
    private readonly IFileStorageRemoteCall _fileStorage;
    public UpdateFileVisibilityCommandHandler(
        IFileStorageRemoteCall fileStorage)
    {
        _fileStorage = fileStorage;
    }

    public override async Task<EmptyResult?> Handle(UpdateFileVisibilityCommand request, CancellationToken ct)
    {

        var r = await _fileStorage.UpdateFileVisibility(
            request.FileId,
            new UpdateFileVisibilityRequest { Visibility = request.Visibility });
        return r.Body;
    }
}
