using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Bff.AdminPanel.Application.Common.Services;

namespace Aizen.Bff.AdminPanel.Application.AdminIdentity.Query;

[DocumentationInfo("Get admin profile detail query handler", "Retrieves profile detail for a given user via the Identity module.")]
public sealed class GetAdminProfileDetailQueryHandler
    : AizenQueryHandler<GetAdminProfileDetailQuery, ProfileDetailResult>
{
    private readonly IIdentityAdminBffRemoteCall _identity;
    private readonly IAdminPanelBffKeycloakServiceTokenProvider _serviceTokenProvider;

    public GetAdminProfileDetailQueryHandler(IIdentityAdminBffRemoteCall identity,
        IAdminPanelBffKeycloakServiceTokenProvider serviceTokenProvider)
    {
        _identity = identity;
        _serviceTokenProvider = serviceTokenProvider;
    }

    public override async Task<ProfileDetailResult?> Handle(
        GetAdminProfileDetailQuery request, CancellationToken cancellationToken)
    {
        var serviceToken = await _serviceTokenProvider.GetAccessTokenAsync(cancellationToken);
        var authHeader = $"Bearer {serviceToken}";

        var result = await _identity.GetProfileById(request.ProfileId, authHeader, request.UserToken);
        return result.Body;
    }
}
