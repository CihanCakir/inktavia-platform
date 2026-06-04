using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Response;

namespace Aizen.Bff.AdminPanel.Application.Authentication.Command;

[DocumentationInfo("Refresh command handler", "Proxies token refresh request to the Identity module auth endpoint.")]
public sealed class RefreshCommandHandler : AizenCommandHandler<RefreshCommand, UserLoginResponse>
{
    private readonly IIdentityAdminBffRemoteCall _identity;
    public RefreshCommandHandler(IIdentityAdminBffRemoteCall identity) { _identity = identity; }

    public override async Task<UserLoginResponse?> Handle(RefreshCommand request, CancellationToken ct)
    {
        var r = await _identity.Refresh(request.Request);
        return r.Body;
    }
}
