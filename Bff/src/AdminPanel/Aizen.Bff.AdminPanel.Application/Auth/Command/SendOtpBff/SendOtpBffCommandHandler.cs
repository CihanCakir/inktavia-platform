using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.Auth.Command;

[DocumentationInfo("SendOtp command handler", "Sends an OTP to the user's phone via the Identity module.")]
public sealed class SendOtpCommandHandler : AizenCommandHandler<SendOtpBffCommand, SendOtpDto>
{
    private readonly IIdentityRemoteCall _identity;

    public SendOtpCommandHandler(
        IIdentityRemoteCall identity)
    {
        _identity = identity;
    }

    public override async Task<SendOtpDto?> Handle(SendOtpBffCommand request, CancellationToken ct)
    {

        var r = await _identity.SendOtp(request.Request);
        return r.Body;
    }
}
