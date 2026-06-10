using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Bff.AdminPanel.Application.Common.Services;

namespace Aizen.Bff.AdminPanel.Application.AdminIdentity.Query;

[DocumentationInfo("SearchOrganizerProfiles query handler", "Returns a paged list of organizer profiles from the Identity module.")]
public sealed class SearchOrganizerProfilesQueryHandler : AizenQueryHandler<SearchOrganizerProfilesQuery, PagedOrganizerProfileResult>
{
    private readonly IIdentityAdminBffRemoteCall _identity;
    private readonly IAdminPanelBffKeycloakServiceTokenProvider _serviceTokenProvider;
    public SearchOrganizerProfilesQueryHandler(
        IIdentityAdminBffRemoteCall identity,
        IAdminPanelBffKeycloakServiceTokenProvider serviceTokenProvider)
    {
        _identity = identity;
        _serviceTokenProvider = serviceTokenProvider;
    }

    public override async Task<PagedOrganizerProfileResult?> Handle(SearchOrganizerProfilesQuery request, CancellationToken ct)
    {
        var serviceToken = await _serviceTokenProvider.GetAccessTokenAsync(ct);
        var authHeader = $"Bearer {serviceToken}";

        var r = await _identity.SearchOrganizerProfiles(authHeader, request.UserToken, request.PageIndex, request.PageSize);
        return r.Body;
    }
}
