using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.Authentication.Command;

[DocumentationInfo("ChangePassword command handler", "Proxies password change request to the Identity module auth endpoint.")]
public sealed class ChangePasswordCommandHandler : AizenCommandHandler<ChangePasswordCommand, ChangePasswordDto>
{
    private readonly IIdentityAdminBffRemoteCall _identity;
    public ChangePasswordCommandHandler(
        IIdentityAdminBffRemoteCall identity)
    {
        _identity = identity;
    }

    public override async Task<ChangePasswordDto?> Handle(ChangePasswordCommand request, CancellationToken ct)
    {

        var r = await _identity.ChangePassword(request.Request);
        return r.Body;
    }
}
