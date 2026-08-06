using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Identity.Abstraction.Response;

namespace Aizen.Bff.AdminPanel.Application.Auth.Command;

[DocumentationInfo("LoginWithPhone command handler", "Proxies phone+password login request to the Identity module auth endpoint. Propagates Identity error envelope to the caller on failure.")]
public sealed class LoginWithPhoneCommandHandler : AizenCommandHandler<LoginWithPhoneBffCommand, UserLoginResponse>
{
    private readonly IIdentityRemoteCall _identity;

    public LoginWithPhoneCommandHandler(
        IIdentityRemoteCall identity)
    {
        _identity = identity;
    }

    public override async Task<UserLoginResponse?> Handle(LoginWithPhoneBffCommand request, CancellationToken ct)
    {

        var r = await _identity.LoginWithPhone(request.Request);

        if (r?.Header is { IsSuccess: false })
            throw new AizenBusinessException(r.Header.ErrorCode);

        return r?.Body;
    }
}
