using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminIdentity.Query;

[DocumentationInfo("SearchVenueProfiles query handler", "Returns a paged list of venue profiles from the Identity module.")]
public sealed class SearchVenueProfilesQueryHandler : AizenQueryHandler<SearchVenueProfilesQuery, PagedVenueProfileResult>
{
    private readonly IIdentityAdminBffRemoteCall _identity;
    public SearchVenueProfilesQueryHandler(IIdentityAdminBffRemoteCall identity) { _identity = identity; }

    public override async Task<PagedVenueProfileResult?> Handle(SearchVenueProfilesQuery request, CancellationToken ct)
    {
        var r = await _identity.SearchVenueProfiles(request.Authorization, request.UserToken, request.PageIndex, request.PageSize);
        return r.Body;
    }
}
