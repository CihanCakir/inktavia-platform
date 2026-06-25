using Aizen.Bff.AdminPanel.Application.AdminFiles.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminFiles.Query;

public sealed class GetAdminFileReviewOverviewQuery : AizenQuery<AdminFileReviewOverviewResponse>
{
    public long FileId { get; }
    public string UserToken { get; }
    public GetAdminFileReviewOverviewQuery(long fileId, string userToken)
    {
        FileId = fileId;
        UserToken = userToken;
    }
}
