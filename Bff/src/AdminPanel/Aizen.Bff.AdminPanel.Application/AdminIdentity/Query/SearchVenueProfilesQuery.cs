using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminIdentity.Query;

public sealed class SearchVenueProfilesQuery : AizenQuery<PagedVenueProfileResult>
{
    public string Authorization { get; }
    public string UserToken { get; }
    public int PageIndex { get; }
    public int PageSize { get; }
    public SearchVenueProfilesQuery(string authorization, string userToken, int pageIndex, int pageSize)
    {
        Authorization = authorization;
        UserToken = userToken;
        PageIndex = pageIndex;
        PageSize = pageSize;
    }
}
