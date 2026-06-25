using Aizen.Bff.AdminPanel.Application.AdminFiles.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Core.CQRS.Handler;
using Aizen.Bff.AdminPanel.Application.Common.Services;

namespace Aizen.Bff.AdminPanel.Application.AdminFiles.Query;

[DocumentationInfo("Get admin file review overview query handler", "Fetches file metadata for the admin file review screen.")]
public sealed class GetAdminFileReviewOverviewQueryHandler
    : AizenQueryHandler<GetAdminFileReviewOverviewQuery, AdminFileReviewOverviewResponse>
{
    private readonly IFileStorageAdminBffRemoteCall _fileStorage;
    private readonly IAdminPanelBffKeycloakServiceTokenProvider _serviceTokenProvider;

    public GetAdminFileReviewOverviewQueryHandler(IFileStorageAdminBffRemoteCall fileStorage,
        IAdminPanelBffKeycloakServiceTokenProvider serviceTokenProvider)
    {
        _fileStorage = fileStorage;
        _serviceTokenProvider = serviceTokenProvider;
    }

    public override async Task<AdminFileReviewOverviewResponse?> Handle(
        GetAdminFileReviewOverviewQuery request, CancellationToken cancellationToken)
    {
        var response = new AdminFileReviewOverviewResponse();

        try
        {
            var serviceToken = await _serviceTokenProvider.GetAccessTokenAsync(cancellationToken);
        var authHeader = $"Bearer {serviceToken}";

        var result = await _fileStorage.GetFileMetadata(request.FileId, authHeader, request.UserToken);
            response.FileMetadata = result.Body;
        }
        catch
        {
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("FileStorage"));
        }

        return response;
    }
}
