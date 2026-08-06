using Aizen.Bff.AdminPanel.Application.Files.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Files.Query;

public sealed class GetFileReviewOverviewBffQuery : AizenQuery<AdminFileReviewOverviewResponse>
{
    public Guid FileId { get; }
    public GetFileReviewOverviewBffQuery(Guid fileId)
    {
        FileId = fileId;
    }
}
