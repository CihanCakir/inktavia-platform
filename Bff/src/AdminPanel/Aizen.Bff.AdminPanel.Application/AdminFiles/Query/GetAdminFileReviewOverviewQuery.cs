using Aizen.Bff.AdminPanel.Application.AdminFiles.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminFiles.Query;

public sealed class GetAdminFileReviewOverviewQuery : AizenQuery<AdminFileReviewOverviewResponse>
{
    public long FileId { get; }
    public string Authorization { get; }
    public string UserToken { get; }
    public GetAdminFileReviewOverviewQuery(long fileId, string authorization, string userToken)
    {
        FileId = fileId;
        Authorization = authorization;
        UserToken = userToken;
    }
}
