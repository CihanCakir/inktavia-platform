using Aizen.Bff.AdminPanel.Application.Common.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Bff.AdminPanel.Application.Common.Services;

namespace Aizen.Bff.AdminPanel.Application.AdminFiles.Command;

[DocumentationInfo("Delete file command handler", "Soft-deletes a file via the FileStorage module admin endpoint.")]
public sealed class DeleteFileCommandHandler
    : AizenCommandHandler<DeleteFileCommand, AdminBffCommandResultDto>
{
    private readonly IFileStorageAdminBffRemoteCall _fileStorage;
    private readonly IAdminPanelBffKeycloakServiceTokenProvider _serviceTokenProvider;

    public DeleteFileCommandHandler(IFileStorageAdminBffRemoteCall fileStorage,
        IAdminPanelBffKeycloakServiceTokenProvider serviceTokenProvider)
    {
        _fileStorage = fileStorage;
        _serviceTokenProvider = serviceTokenProvider;
    }

    public override async Task<AdminBffCommandResultDto?> Handle(
        DeleteFileCommand request, CancellationToken cancellationToken)
    {
        var serviceToken = await _serviceTokenProvider.GetAccessTokenAsync(cancellationToken);
        var authHeader = $"Bearer {serviceToken}";

        var result = await _fileStorage.DeleteFile(request.FileId, authHeader, request.UserToken);

        return result.Header.IsSuccess
            ? AdminBffCommandResultDto.Ok()
            : AdminBffCommandResultDto.Fail(result.Header.ErrorMessage ?? "File deletion failed.");
    }
}
