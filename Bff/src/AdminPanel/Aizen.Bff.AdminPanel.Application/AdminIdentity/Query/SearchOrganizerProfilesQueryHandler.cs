using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminIdentity.Query;

[DocumentationInfo("SearchOrganizerProfiles query handler", "Returns a paged list of organizer profiles from the Identity module.")]
public sealed class SearchOrganizerProfilesQueryHandler : AizenQueryHandler<SearchOrganizerProfilesQuery, PagedOrganizerProfileResult>
{
    private readonly IIdentityAdminBffRemoteCall _identity;
    public SearchOrganizerProfilesQueryHandler(IIdentityAdminBffRemoteCall identity) { _identity = identity; }

    public override async Task<PagedOrganizerProfileResult?> Handle(SearchOrganizerProfilesQuery request, CancellationToken ct)
    {
        var r = await _identity.SearchOrganizerProfiles(request.Authorization, request.UserToken, request.PageIndex, request.PageSize);
        return r.Body;
    }
}
