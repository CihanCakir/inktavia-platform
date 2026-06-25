using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Bff.AdminPanel.Application.Common.Services;

namespace Aizen.Bff.AdminPanel.Application.AdminIdentity.Query;

[DocumentationInfo("GetOrganizerProfileById query handler", "Returns a single organizer profile by ID from the Identity module.")]
public sealed class GetOrganizerProfileByIdQueryHandler : AizenQueryHandler<GetOrganizerProfileByIdQuery, OrganizerProfileResult>
{
    private readonly IIdentityAdminBffRemoteCall _identity;
    private readonly IAdminPanelBffKeycloakServiceTokenProvider _serviceTokenProvider;
    public GetOrganizerProfileByIdQueryHandler(
        IIdentityAdminBffRemoteCall identity,
        IAdminPanelBffKeycloakServiceTokenProvider serviceTokenProvider)
    {
        _identity = identity;
        _serviceTokenProvider = serviceTokenProvider;
    }

    public override async Task<OrganizerProfileResult?> Handle(GetOrganizerProfileByIdQuery request, CancellationToken ct)
    {
        var serviceToken = await _serviceTokenProvider.GetAccessTokenAsync(ct);
        var authHeader = $"Bearer {serviceToken}";

        var r = await _identity.GetOrganizerProfileById(request.ProfileId, authHeader, request.UserToken);
        return r.Body;
    }
}
