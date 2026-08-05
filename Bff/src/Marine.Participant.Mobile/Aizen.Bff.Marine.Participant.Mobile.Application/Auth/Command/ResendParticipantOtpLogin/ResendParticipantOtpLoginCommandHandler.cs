using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Auth.OtpLogin;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Dto.OtpLogin;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Auth;

public sealed class ResendParticipantOtpLoginCommandHandler
    : AizenCommandHandler<ResendParticipantOtpLoginCommand, MobileOtpResendResponse>
{
    private readonly IIdentityRemoteCall _identity;
    private readonly ILogger<ResendParticipantOtpLoginCommandHandler> _logger;

    public ResendParticipantOtpLoginCommandHandler(
        IIdentityRemoteCall identity, ILogger<ResendParticipantOtpLoginCommandHandler> logger)
    { _identity = identity; _logger = logger; }

    public override async Task<MobileOtpResendResponse?> Handle(
        ResendParticipantOtpLoginCommand request, CancellationToken ct)
    {
        try
        {
            var result = await _identity.ResendParticipantOtpLogin(new ResendProviderOtpLoginRequest
            { LoginRequestId = request.LoginRequestId });
            var data = result.Body;
            if (data is not null)
                return new MobileOtpResendResponse
                {
                    Resent = data.Resent,
                    ResendAfterSeconds = data.ResendAfterSeconds,
                    ExpiresInSeconds = data.ExpiresInSeconds,
                    Message = data.Message,
                };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Participant OTP login resend failed.");
        }

        return new MobileOtpResendResponse { Resent = false, Message = "Please try again later." };
    }
}
