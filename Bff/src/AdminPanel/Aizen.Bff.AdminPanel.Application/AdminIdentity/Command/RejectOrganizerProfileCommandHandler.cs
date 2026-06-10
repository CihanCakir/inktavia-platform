using Aizen.Bff.AdminPanel.Application.Common.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Request;
using Aizen.Bff.AdminPanel.Application.Common.Services;

namespace Aizen.Bff.AdminPanel.Application.AdminIdentity.Command;

[DocumentationInfo("Reject organizer profile command handler", "Rejects an organizer profile with a reason via the Identity module admin endpoint.")]
public sealed class RejectOrganizerProfileCommandHandler
    : AizenCommandHandler<RejectOrganizerProfileCommand, AdminBffCommandResultDto>
{
    private readonly IIdentityAdminBffRemoteCall _identity;
    private readonly IAdminPanelBffKeycloakServiceTokenProvider _serviceTokenProvider;

    public RejectOrganizerProfileCommandHandler(IIdentityAdminBffRemoteCall identity,
        IAdminPanelBffKeycloakServiceTokenProvider serviceTokenProvider)
    {
        _identity = identity;
        _serviceTokenProvider = serviceTokenProvider;
    }

    public override async Task<AdminBffCommandResultDto?> Handle(
        RejectOrganizerProfileCommand request, CancellationToken cancellationToken)
    {
        var serviceToken = await _serviceTokenProvider.GetAccessTokenAsync(cancellationToken);
        var authHeader = $"Bearer {serviceToken}";

        var result = await _identity.RejectOrganizerProfile(
            request.UserId,
            request.ProfileId,
            new RejectProfileRequest { Reason = request.Reason },
            authHeader,
            request.UserToken);

        return result.Header.IsSuccess
            ? AdminBffCommandResultDto.Ok()
            : AdminBffCommandResultDto.Fail(result.Header.ErrorMessage ?? "Rejection failed.");
    }
}
