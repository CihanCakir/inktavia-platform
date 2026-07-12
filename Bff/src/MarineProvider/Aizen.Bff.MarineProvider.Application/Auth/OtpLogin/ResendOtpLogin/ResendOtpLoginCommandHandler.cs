using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Contracts.Auth.OtpLogin;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Dto.OtpLogin;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.MarineProvider.Application.Auth.OtpLogin.ResendOtpLogin;

public sealed class ResendOtpLoginCommandHandler
    : AizenCommandHandler<ResendOtpLoginCommand, OtpLoginResendResponse>
{
    private readonly IProviderIdentityRemoteCall _identity;
    private readonly ILogger<ResendOtpLoginCommandHandler> _logger;

    public ResendOtpLoginCommandHandler(IProviderIdentityRemoteCall identity, ILogger<ResendOtpLoginCommandHandler> logger)
    { _identity = identity; _logger = logger; }

    public override async Task<OtpLoginResendResponse?> Handle(ResendOtpLoginCommand request, CancellationToken ct)
    {
        try
        {
            var result = await _identity.ResendProviderOtpLogin(new ResendProviderOtpLoginRequest { LoginRequestId = request.LoginRequestId });
            var data = result.Body;
            if (data is not null)
                return new OtpLoginResendResponse
                {
                    Resent = data.Resent, ResendAfterSeconds = data.ResendAfterSeconds,
                    ExpiresInSeconds = data.ExpiresInSeconds,
                    // Pass Identity's message through: a cooldown-blocked resend reports Resent=false
                    // and says so, rather than claiming a new code was sent.
                    Message = data.Message,
                };
        }
        catch (Exception ex) { _logger.LogError(ex, "Identity OTP login resend failed."); }
        return new OtpLoginResendResponse { Resent = true, Message = "If an account exists, a new verification code has been sent." };
    }
}
