using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Identity.Abstraction.Response;

namespace Aizen.Bff.AdminPanel.Application.Auth.Command;

[DocumentationInfo("Refresh command handler", "Proxies token refresh request to the Identity module auth endpoint. Propagates Identity error envelope to the caller on failure.")]
public sealed class RefreshCommandHandler : AizenCommandHandler<RefreshBffCommand, UserLoginResponse>
{
    private readonly IIdentityRemoteCall _identity;

    public RefreshCommandHandler(
        IIdentityRemoteCall identity)
    {
        _identity = identity;
    }

    public override async Task<UserLoginResponse?> Handle(RefreshBffCommand request, CancellationToken ct)
    {

        var r = await _identity.Refresh(request.Request);

        if (r?.Header is { IsSuccess: false })
            throw new AizenBusinessException(r.Header.ErrorCode);

        return r?.Body;
    }
}
