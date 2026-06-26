using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminIdentity.Query;

public sealed class SearchOrganizerProfilesQuery : AizenQuery<PagedOrganizerProfileResult>
{
    public int PageIndex { get; }
    public int PageSize { get; }
    public SearchOrganizerProfilesQuery(int pageIndex, int pageSize)
    {
        PageIndex = pageIndex;
        PageSize = pageSize;
    }
}
