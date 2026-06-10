using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Bff.AdminPanel.Application.Common.Services;

namespace Aizen.Bff.AdminPanel.Application.AdminIdentity.Query;

[DocumentationInfo("SearchParticipantProfiles query handler", "Returns a paged list of participant profiles from the Identity module.")]
public sealed class SearchParticipantProfilesQueryHandler : AizenQueryHandler<SearchParticipantProfilesQuery, PagedParticipantProfileResult>
{
    private readonly IIdentityAdminBffRemoteCall _identity;
    private readonly IAdminPanelBffKeycloakServiceTokenProvider _serviceTokenProvider;
    public SearchParticipantProfilesQueryHandler(
        IIdentityAdminBffRemoteCall identity,
        IAdminPanelBffKeycloakServiceTokenProvider serviceTokenProvider)
    {
        _identity = identity;
        _serviceTokenProvider = serviceTokenProvider;
    }

    public override async Task<PagedParticipantProfileResult?> Handle(SearchParticipantProfilesQuery request, CancellationToken ct)
    {
        var serviceToken = await _serviceTokenProvider.GetAccessTokenAsync(ct);
        var authHeader = $"Bearer {serviceToken}";

        var r = await _identity.SearchParticipantProfiles(authHeader, request.UserToken, request.PageIndex, request.PageSize);
        return r.Body;
    }
}
