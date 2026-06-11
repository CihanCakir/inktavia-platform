using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Services;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Response;

namespace Aizen.Bff.AdminPanel.Application.Authentication.Command;

[DocumentationInfo("LoginWithUsername command handler", "Proxies username+pin login request to the Identity module auth endpoint.")]
public sealed class LoginWithUsernameCommandHandler : AizenCommandHandler<LoginWithUsernameCommand, UserLoginResponse>
{
    private readonly IIdentityAdminBffRemoteCall _identity;
    private readonly IAdminPanelBffKeycloakServiceTokenProvider _serviceTokenProvider;

    public LoginWithUsernameCommandHandler(
        IIdentityAdminBffRemoteCall identity,
        IAdminPanelBffKeycloakServiceTokenProvider serviceTokenProvider)
    {
        _identity = identity;
        _serviceTokenProvider = serviceTokenProvider;
    }

    public override async Task<UserLoginResponse?> Handle(LoginWithUsernameCommand request, CancellationToken ct)
    {
        var serviceToken = await _serviceTokenProvider.GetAccessTokenAsync(ct);
        var authHeader = $"Bearer {serviceToken}";

        var r = await _identity.LoginWithUsername(request.Request, authHeader);
        return r.Body;
    }
}
