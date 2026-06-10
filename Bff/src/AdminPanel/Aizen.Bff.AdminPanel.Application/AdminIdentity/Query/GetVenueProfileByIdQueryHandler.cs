using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Bff.AdminPanel.Application.Common.Services;

namespace Aizen.Bff.AdminPanel.Application.AdminIdentity.Query;

[DocumentationInfo("GetVenueProfileById query handler", "Returns a single venue profile by ID from the Identity module.")]
public sealed class GetVenueProfileByIdQueryHandler : AizenQueryHandler<GetVenueProfileByIdQuery, VenueProfileResult>
{
    private readonly IIdentityAdminBffRemoteCall _identity;
    private readonly IAdminPanelBffKeycloakServiceTokenProvider _serviceTokenProvider;
    public GetVenueProfileByIdQueryHandler(
        IIdentityAdminBffRemoteCall identity,
        IAdminPanelBffKeycloakServiceTokenProvider serviceTokenProvider)
    {
        _identity = identity;
        _serviceTokenProvider = serviceTokenProvider;
    }

    public override async Task<VenueProfileResult?> Handle(GetVenueProfileByIdQuery request, CancellationToken ct)
    {
        var serviceToken = await _serviceTokenProvider.GetAccessTokenAsync(ct);
        var authHeader = $"Bearer {serviceToken}";

        var r = await _identity.GetVenueProfileById(request.ProfileId, authHeader, request.UserToken);
        return r.Body;
    }
}
