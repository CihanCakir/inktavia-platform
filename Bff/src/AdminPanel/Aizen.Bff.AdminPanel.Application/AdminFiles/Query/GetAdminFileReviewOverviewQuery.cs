using Aizen.Bff.AdminPanel.Application.Common;
using Aizen.Bff.AdminPanel.Application.AdminFiles.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Core.CQRS.Message;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminFiles.Query;

public sealed class GetAdminFileReviewOverviewQuery : AizenQuery<AdminFileReviewOverviewResponse>
{
    public Guid FileId { get; }
    public string Authorization { get; }
    public GetAdminFileReviewOverviewQuery(Guid fileId, string authorization)
    {
        FileId = fileId;
        Authorization = authorization;
    }
}

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
            var result = await _fileStorage.GetFileMetadata(request.FileId, request.Authorization);
            response.FileMetadata = result.Body;
        }
        catch
        {
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("FileStorage"));
        }

        return response;
    }
}
