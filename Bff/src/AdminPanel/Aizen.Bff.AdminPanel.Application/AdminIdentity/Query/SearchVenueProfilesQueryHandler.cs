using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Bff.AdminPanel.Application.Common.Services;

namespace Aizen.Bff.AdminPanel.Application.AdminIdentity.Query;

[DocumentationInfo("SearchVenueProfiles query handler", "Returns a paged list of venue profiles from the Identity module.")]
public sealed class SearchVenueProfilesQueryHandler : AizenQueryHandler<SearchVenueProfilesQuery, PagedVenueProfileResult>
{
    private readonly IIdentityAdminBffRemoteCall _identity;
    private readonly IAdminPanelBffKeycloakServiceTokenProvider _serviceTokenProvider;
    public SearchVenueProfilesQueryHandler(
        IIdentityAdminBffRemoteCall identity,
        IAdminPanelBffKeycloakServiceTokenProvider serviceTokenProvider)
    {
        _identity = identity;
        _serviceTokenProvider = serviceTokenProvider;
    }

    public override async Task<PagedVenueProfileResult?> Handle(SearchVenueProfilesQuery request, CancellationToken ct)
    {
        var serviceToken = await _serviceTokenProvider.GetAccessTokenAsync(ct);
        var authHeader = $"Bearer {serviceToken}";

        var r = await _identity.SearchVenueProfiles(authHeader, request.UserToken, request.PageIndex, request.PageSize);
        return r.Body;
    }
}
