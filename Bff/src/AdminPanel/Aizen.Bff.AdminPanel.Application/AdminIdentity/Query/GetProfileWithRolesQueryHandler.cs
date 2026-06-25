using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Bff.AdminPanel.Application.Common.Services;

namespace Aizen.Bff.AdminPanel.Application.AdminIdentity.Query;

[DocumentationInfo("GetProfileWithRoles query handler", "Returns a user profile with its assigned roles from the Identity module.")]
public sealed class GetProfileWithRolesQueryHandler : AizenQueryHandler<GetProfileWithRolesQuery, ProfileWithRolesResult>
{
    private readonly IIdentityAdminBffRemoteCall _identity;
    private readonly IAdminPanelBffKeycloakServiceTokenProvider _serviceTokenProvider;
    public GetProfileWithRolesQueryHandler(
        IIdentityAdminBffRemoteCall identity,
        IAdminPanelBffKeycloakServiceTokenProvider serviceTokenProvider)
    {
        _identity = identity;
        _serviceTokenProvider = serviceTokenProvider;
    }

    public override async Task<ProfileWithRolesResult?> Handle(GetProfileWithRolesQuery request, CancellationToken ct)
    {
        var serviceToken = await _serviceTokenProvider.GetAccessTokenAsync(ct);
        var authHeader = $"Bearer {serviceToken}";

        var r = await _identity.GetProfileWithRoles(request.ProfileId, authHeader, request.UserToken);
        return r.Body;
    }
}
