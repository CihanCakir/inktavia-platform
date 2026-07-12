using Aizen.Bff.AdminPanel.Application.AdminFiles.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminFiles.Query;

public sealed class GetAdminFileReviewOverviewQuery : AizenQuery<AdminFileReviewOverviewResponse>
{
    public Guid FileId { get; }
    public GetAdminFileReviewOverviewQuery(Guid fileId)
    {
        FileId = fileId;
    }
}
