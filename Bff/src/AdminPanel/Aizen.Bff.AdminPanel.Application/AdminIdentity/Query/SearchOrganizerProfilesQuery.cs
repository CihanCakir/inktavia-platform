using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminIdentity.Query;

public sealed class SearchOrganizerProfilesQuery : AizenQuery<PagedOrganizerProfileResult>
{
    public string UserToken { get; }
    public int PageIndex { get; }
    public int PageSize { get; }
    public SearchOrganizerProfilesQuery(string userToken, int pageIndex, int pageSize)
    {
        UserToken = userToken;
        PageIndex = pageIndex;
        PageSize = pageSize;
    }
}
