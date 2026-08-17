using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Response;

namespace Aizen.Bff.AdminPanel.Application.Authentication.Command;

[DocumentationInfo("LoginWithOtp command handler", "Proxies OTP-based login request to the Identity module auth endpoint.")]
public sealed class LoginWithOtpCommandHandler : AizenCommandHandler<LoginWithOtpCommand, UserLoginResponse>
{
    private readonly IIdentityAdminBffRemoteCall _identity;

    public LoginWithOtpCommandHandler(
        IIdentityAdminBffRemoteCall identity)
    {
        _identity = identity;
    }

    public override async Task<UserLoginResponse?> Handle(LoginWithOtpCommand request, CancellationToken ct)
    {

        var r = await _identity.LoginWithOtp(request.Request);
        return r.Body;
    }
}
