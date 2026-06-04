using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Response;

namespace Aizen.Bff.AdminPanel.Application.Authentication.Command;

[DocumentationInfo("LoginWithUsername command handler", "Proxies username+pin login request to the Identity module auth endpoint.")]
public sealed class LoginWithUsernameCommandHandler : AizenCommandHandler<LoginWithUsernameCommand, UserLoginResponse>
{
    private readonly IIdentityAdminBffRemoteCall _identity;
    public LoginWithUsernameCommandHandler(IIdentityAdminBffRemoteCall identity) { _identity = identity; }

    public override async Task<UserLoginResponse?> Handle(LoginWithUsernameCommand request, CancellationToken ct)
    {
        var r = await _identity.LoginWithUsername(request.Request);
        return r.Body;
    }
}
