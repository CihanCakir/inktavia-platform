using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.Auth.Command;

[DocumentationInfo("CheckOtp command handler", "Validates an OTP code via the Identity module.")]
public sealed class CheckOtpCommandHandler : AizenCommandHandler<CheckOtpBffCommand, CheckOtpDto>
{
    private readonly IIdentityRemoteCall _identity;

    public CheckOtpCommandHandler(
        IIdentityRemoteCall identity)
    {
        _identity = identity;
    }

    public override async Task<CheckOtpDto?> Handle(CheckOtpBffCommand request, CancellationToken ct)
    {

        var r = await _identity.CheckOtp(request.Request);
        return r.Body;
    }
}
