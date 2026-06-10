using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.FileStorage.Abstraction.RemoteCall.File.Requests;
using Aizen.Bff.AdminPanel.Application.Common.Services;

namespace Aizen.Bff.AdminPanel.Application.AdminFiles.Command;

[DocumentationInfo("CreateFileReadUrl command handler", "Generates a pre-signed read URL for a file via the FileStorage module.")]
public sealed class CreateFileReadUrlCommandHandler : AizenCommandHandler<CreateFileReadUrlCommand, FileAccessUrlResult>
{
    private readonly IFileStorageAdminBffRemoteCall _fileStorage;
    private readonly IAdminPanelBffKeycloakServiceTokenProvider _serviceTokenProvider;
    public CreateFileReadUrlCommandHandler(
        IFileStorageAdminBffRemoteCall fileStorage,
        IAdminPanelBffKeycloakServiceTokenProvider serviceTokenProvider)
    {
        _fileStorage = fileStorage;
        _serviceTokenProvider = serviceTokenProvider;
    }

    public override async Task<FileAccessUrlResult?> Handle(CreateFileReadUrlCommand request, CancellationToken ct)
    {
        var serviceToken = await _serviceTokenProvider.GetAccessTokenAsync(ct);
        var authHeader = $"Bearer {serviceToken}";

        var r = await _fileStorage.CreateReadUrl(
            request.FileId,
            new CreateFileReadUrlRemoteCallRequest { ExpiresIn = TimeSpan.FromMinutes(request.ExpiresInMinutes) },
            authHeader,
            request.UserToken);
        return r.Body;
    }
}
