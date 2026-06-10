using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Bff.AdminPanel.Application.Common.Services;

namespace Aizen.Bff.AdminPanel.Application.AdminIdentity.Query;

[DocumentationInfo("GetParticipantProfileById query handler", "Returns a single participant profile by ID from the Identity module.")]
public sealed class GetParticipantProfileByIdQueryHandler : AizenQueryHandler<GetParticipantProfileByIdQuery, ParticipantProfileResult>
{
    private readonly IIdentityAdminBffRemoteCall _identity;
    private readonly IAdminPanelBffKeycloakServiceTokenProvider _serviceTokenProvider;
    public GetParticipantProfileByIdQueryHandler(
        IIdentityAdminBffRemoteCall identity,
        IAdminPanelBffKeycloakServiceTokenProvider serviceTokenProvider)
    {
        _identity = identity;
        _serviceTokenProvider = serviceTokenProvider;
    }

    public override async Task<ParticipantProfileResult?> Handle(GetParticipantProfileByIdQuery request, CancellationToken ct)
    {
        var serviceToken = await _serviceTokenProvider.GetAccessTokenAsync(ct);
        var authHeader = $"Bearer {serviceToken}";

        var r = await _identity.GetParticipantProfileById(request.ProfileId, authHeader, request.UserToken);
        return r.Body;
    }
}
