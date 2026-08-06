using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.Auth.Command;

[DocumentationInfo("ChangePassword command handler", "Proxies password change request to the Identity module auth endpoint.")]
public sealed class ChangePasswordCommandHandler : AizenCommandHandler<ChangePasswordBffCommand, ChangePasswordDto>
{
    private readonly IIdentityRemoteCall _identity;
    public ChangePasswordCommandHandler(
        IIdentityRemoteCall identity)
    {
        _identity = identity;
    }

    public override async Task<ChangePasswordDto?> Handle(ChangePasswordBffCommand request, CancellationToken ct)
    {

        var r = await _identity.ChangePassword(request.Request);
        return r.Body;
    }
}
