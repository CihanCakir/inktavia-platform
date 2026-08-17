using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Contracts.Auth.Password;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Dto.PasswordRecovery;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.MarineProvider.Application.Auth;

/// <summary>
/// Delegates OTP resend to Identity via service-token remote call.
/// Maps the Identity response into the existing BFF response contract (frontend unchanged).
/// </summary>
public sealed class ResendProviderPasswordOtpCommandHandler
    : AizenCommandHandler<ResendProviderPasswordOtpCommand, ResendProviderPasswordOtpResponse>
{
    private readonly IIdentityRemoteCall _identity;
    private readonly ILogger<ResendProviderPasswordOtpCommandHandler> _logger;

    public ResendProviderPasswordOtpCommandHandler(
        IIdentityRemoteCall identity,
        ILogger<ResendProviderPasswordOtpCommandHandler> logger)
    {
        _identity = identity;
        _logger = logger;
    }

    public override async Task<ResendProviderPasswordOtpResponse?> Handle(
        ResendProviderPasswordOtpCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _identity.ResendProviderPasswordRecoveryOtp(
                new ResendProviderPasswordRecoveryOtpRequest
                {
                    ResetRequestId = request.ResetRequestId,
                });

            var data = result.Body;
            if (data is not null)
            {
                return new ResendProviderPasswordOtpResponse
                {
                    Resent = data.Resent,
                    ResendAfterSeconds = data.ResendAfterSeconds,
                    ExpiresInSeconds = data.ExpiresInSeconds,
                    Message = data.Message,
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Identity OTP resend failed.");
            return new ResendProviderPasswordOtpResponse
            {
                Resent = false,
                Message = "Service temporarily unavailable. Please try again later.",
            };
        }

        // Identity returned a null body — treat as unknown account (anti-enumeration).
        return new ResendProviderPasswordOtpResponse
        {
            Resent = true,
            Message = "If an account exists, a new verification code has been sent.",
        };
    }
}
