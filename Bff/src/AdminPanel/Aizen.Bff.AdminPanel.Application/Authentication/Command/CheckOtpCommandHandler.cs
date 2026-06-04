using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.Authentication.Command;

[DocumentationInfo("CheckOtp command handler", "Validates an OTP code via the Identity module.")]
public sealed class CheckOtpCommandHandler : AizenCommandHandler<CheckOtpCommand, CheckOtpDto>
{
    private readonly IIdentityAdminBffRemoteCall _identity;
    public CheckOtpCommandHandler(IIdentityAdminBffRemoteCall identity) { _identity = identity; }

    public override async Task<CheckOtpDto?> Handle(CheckOtpCommand request, CancellationToken ct)
    {
        var r = await _identity.CheckOtp(request.Request);
        return r.Body;
    }
}
