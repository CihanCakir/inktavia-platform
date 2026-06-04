using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Response;

namespace Aizen.Bff.AdminPanel.Application.Authentication.Command;

[DocumentationInfo("LoginWithPhone command handler", "Proxies phone+password login request to the Identity module auth endpoint.")]
public sealed class LoginWithPhoneCommandHandler : AizenCommandHandler<LoginWithPhoneCommand, UserLoginResponse>
{
    private readonly IIdentityAdminBffRemoteCall _identity;
    public LoginWithPhoneCommandHandler(IIdentityAdminBffRemoteCall identity) { _identity = identity; }

    public override async Task<UserLoginResponse?> Handle(LoginWithPhoneCommand request, CancellationToken ct)
    {
        var r = await _identity.LoginWithPhone(request.Request);
        return r.Body;
    }
}
