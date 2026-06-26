using Aizen.Bff.AdminPanel.Application.AdminFiles.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminFiles.Query;

[DocumentationInfo("Get admin file review overview query handler", "Fetches file metadata for the admin file review screen.")]
public sealed class GetAdminFileReviewOverviewQueryHandler
    : AizenQueryHandler<GetAdminFileReviewOverviewQuery, AdminFileReviewOverviewResponse>
{
    private readonly IFileStorageAdminBffRemoteCall _fileStorage;

    public GetAdminFileReviewOverviewQueryHandler(IFileStorageAdminBffRemoteCall fileStorage)
    {
        _fileStorage = fileStorage;
    }

    public override async Task<AdminFileReviewOverviewResponse?> Handle(
        GetAdminFileReviewOverviewQuery request, CancellationToken cancellationToken)
    {
        var response = new AdminFileReviewOverviewResponse();

        try
        {

        var result = await _fileStorage.GetFileMetadata(request.FileId);
            response.FileMetadata = result.Body;
        }
        catch
        {
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("FileStorage"));
        }

        return response;
    }
}
