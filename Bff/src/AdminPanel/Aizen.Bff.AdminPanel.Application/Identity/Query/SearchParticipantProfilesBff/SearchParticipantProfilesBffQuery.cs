using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Identity.Query;

public sealed class SearchParticipantProfilesBffQuery : AizenQuery<PagedParticipantProfileResult>
{
    public int PageIndex { get; }
    public int PageSize { get; }
    public SearchParticipantProfilesBffQuery(int pageIndex, int pageSize)
    {
        PageIndex = pageIndex;
        PageSize = pageSize;
    }
}
