using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.Authentication.Command;

[DocumentationInfo("SendOtp command handler", "Sends an OTP to the user's phone via the Identity module.")]
public sealed class SendOtpCommandHandler : AizenCommandHandler<SendOtpCommand, SendOtpDto>
{
    private readonly IIdentityAdminBffRemoteCall _identity;

    public SendOtpCommandHandler(
        IIdentityAdminBffRemoteCall identity)
    {
        _identity = identity;
    }

    public override async Task<SendOtpDto?> Handle(SendOtpCommand request, CancellationToken ct)
    {

        var r = await _identity.SendOtp(request.Request);
        return r.Body;
    }
}
