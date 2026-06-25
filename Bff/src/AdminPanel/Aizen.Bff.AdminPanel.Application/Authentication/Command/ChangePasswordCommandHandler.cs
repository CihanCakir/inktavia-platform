using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Dto;
using Aizen.Bff.AdminPanel.Application.Common.Services;

namespace Aizen.Bff.AdminPanel.Application.Authentication.Command;

[DocumentationInfo("ChangePassword command handler", "Proxies password change request to the Identity module auth endpoint.")]
public sealed class ChangePasswordCommandHandler : AizenCommandHandler<ChangePasswordCommand, ChangePasswordDto>
{
    private readonly IIdentityAdminBffRemoteCall _identity;
    private readonly IAdminPanelBffKeycloakServiceTokenProvider _serviceTokenProvider;
    public ChangePasswordCommandHandler(
        IIdentityAdminBffRemoteCall identity,
        IAdminPanelBffKeycloakServiceTokenProvider serviceTokenProvider)
    {
        _identity = identity;
        _serviceTokenProvider = serviceTokenProvider;
    }

    public override async Task<ChangePasswordDto?> Handle(ChangePasswordCommand request, CancellationToken ct)
    {
        var serviceToken = await _serviceTokenProvider.GetAccessTokenAsync(ct);
        var authHeader = $"Bearer {serviceToken}";

        var r = await _identity.ChangePassword(request.Request, authHeader, request.UserToken);
        return r.Body;
    }
}
