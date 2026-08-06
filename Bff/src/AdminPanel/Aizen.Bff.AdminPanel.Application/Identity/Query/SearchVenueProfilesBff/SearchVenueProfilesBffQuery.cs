using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Identity.Query;

public sealed class SearchVenueProfilesBffQuery : AizenQuery<PagedVenueProfileResult>
{
    public int PageIndex { get; }
    public int PageSize { get; }
    public SearchVenueProfilesBffQuery(int pageIndex, int pageSize)
    {
        PageIndex = pageIndex;
        PageSize = pageSize;
    }
}
