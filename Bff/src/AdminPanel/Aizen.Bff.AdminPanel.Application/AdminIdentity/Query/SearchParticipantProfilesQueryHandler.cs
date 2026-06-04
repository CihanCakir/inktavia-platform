using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminIdentity.Query;

[DocumentationInfo("SearchParticipantProfiles query handler", "Returns a paged list of participant profiles from the Identity module.")]
public sealed class SearchParticipantProfilesQueryHandler : AizenQueryHandler<SearchParticipantProfilesQuery, PagedParticipantProfileResult>
{
    private readonly IIdentityAdminBffRemoteCall _identity;
    public SearchParticipantProfilesQueryHandler(IIdentityAdminBffRemoteCall identity) { _identity = identity; }

    public override async Task<PagedParticipantProfileResult?> Handle(SearchParticipantProfilesQuery request, CancellationToken ct)
    {
        var r = await _identity.SearchParticipantProfiles(request.Authorization, request.UserToken, request.PageIndex, request.PageSize);
        return r.Body;
    }
}
