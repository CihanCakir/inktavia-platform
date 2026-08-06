using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.Identity.Query;

[DocumentationInfo("SearchVenueProfiles query handler", "Returns a paged list of venue profiles from the Identity module.")]
public sealed class SearchVenueProfilesBffQueryHandler : AizenQueryHandler<SearchVenueProfilesBffQuery, PagedVenueProfileResult>
{
    private readonly IIdentityRemoteCall _identity;
    public SearchVenueProfilesBffQueryHandler(
        IIdentityRemoteCall identity)
    {
        _identity = identity;
    }

    public override async Task<PagedVenueProfileResult?> Handle(SearchVenueProfilesBffQuery request, CancellationToken ct)
    {

        var r = await _identity.SearchVenueProfiles(request.PageIndex, request.PageSize);
        return r.Body;
    }
}
