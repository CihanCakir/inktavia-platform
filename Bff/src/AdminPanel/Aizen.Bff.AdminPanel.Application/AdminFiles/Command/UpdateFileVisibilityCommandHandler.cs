using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Bff.AdminPanel.Application.Common.Services;

namespace Aizen.Bff.AdminPanel.Application.AdminFiles.Command;

[DocumentationInfo("UpdateFileVisibility command handler", "Updates the visibility (public/private) of a file via the FileStorage module.")]
public sealed class UpdateFileVisibilityCommandHandler : AizenCommandHandler<UpdateFileVisibilityCommand, EmptyResult>
{
    private readonly IFileStorageAdminBffRemoteCall _fileStorage;
    private readonly IAdminPanelBffKeycloakServiceTokenProvider _serviceTokenProvider;
    public UpdateFileVisibilityCommandHandler(
        IFileStorageAdminBffRemoteCall fileStorage,
        IAdminPanelBffKeycloakServiceTokenProvider serviceTokenProvider)
    {
        _fileStorage = fileStorage;
        _serviceTokenProvider = serviceTokenProvider;
    }

    public override async Task<EmptyResult?> Handle(UpdateFileVisibilityCommand request, CancellationToken ct)
    {
        var serviceToken = await _serviceTokenProvider.GetAccessTokenAsync(ct);
        var authHeader = $"Bearer {serviceToken}";

        var r = await _fileStorage.UpdateFileVisibility(
            request.FileId,
            new UpdateFileVisibilityRequest { Visibility = request.Visibility },
            authHeader,
            request.UserToken);
        return r.Body;
    }
}
