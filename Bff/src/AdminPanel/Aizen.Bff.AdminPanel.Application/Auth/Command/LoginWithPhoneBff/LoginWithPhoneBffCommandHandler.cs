using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Identity.Abstraction.Dto.OtpLogin;

namespace Aizen.Bff.AdminPanel.Application.Auth.Command;

[DocumentationInfo("LoginWithPhone command handler", "Proxies phone+password credential login to the Identity module, which verifies the password and mints a Keycloak login-ticket handoff (Option 2). Returns the OTP-verify-shaped handoff response.")]
public sealed class LoginWithPhoneCommandHandler : AizenCommandHandler<LoginWithPhoneBffCommand, VerifyProviderOtpLoginResponse>
{
    private readonly IIdentityRemoteCall _identity;

    public LoginWithPhoneCommandHandler(
        IIdentityRemoteCall identity)
    {
        _identity = identity;
    }

    public override async Task<VerifyProviderOtpLoginResponse?> Handle(LoginWithPhoneBffCommand request, CancellationToken ct)
    {
        var r = await _identity.LoginWithPhone(request.Request);

        if (r?.Header is { IsSuccess: false })
            throw new AizenBusinessException(r.Header.ErrorCode);

        return r?.Body;
    }
}
