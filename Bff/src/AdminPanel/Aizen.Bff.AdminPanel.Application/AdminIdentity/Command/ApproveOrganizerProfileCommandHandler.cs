using Aizen.Bff.AdminPanel.Application.Common.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Bff.AdminPanel.Application.Common.Services;

namespace Aizen.Bff.AdminPanel.Application.AdminIdentity.Command;

[DocumentationInfo("Approve organizer profile command handler", "Approves an organizer profile via the Identity module admin endpoint.")]
public sealed class ApproveOrganizerProfileCommandHandler
    : AizenCommandHandler<ApproveOrganizerProfileCommand, AdminBffCommandResultDto>
{
    private readonly IIdentityAdminBffRemoteCall _identity;
    private readonly IAdminPanelBffKeycloakServiceTokenProvider _serviceTokenProvider;

    public ApproveOrganizerProfileCommandHandler(IIdentityAdminBffRemoteCall identity,
        IAdminPanelBffKeycloakServiceTokenProvider serviceTokenProvider)
    {
        _identity = identity;
        _serviceTokenProvider = serviceTokenProvider;
    }

    public override async Task<AdminBffCommandResultDto?> Handle(
        ApproveOrganizerProfileCommand request, CancellationToken cancellationToken)
    {
        var serviceToken = await _serviceTokenProvider.GetAccessTokenAsync(cancellationToken);
        var authHeader = $"Bearer {serviceToken}";

        var result = await _identity.ApproveOrganizerProfile(
            request.UserId,
            request.ProfileId,
            authHeader,
            request.UserToken);

        return result.Header.IsSuccess
            ? AdminBffCommandResultDto.Ok()
            : AdminBffCommandResultDto.Fail(result.Header.ErrorMessage ?? "Approval failed.");
    }
}
