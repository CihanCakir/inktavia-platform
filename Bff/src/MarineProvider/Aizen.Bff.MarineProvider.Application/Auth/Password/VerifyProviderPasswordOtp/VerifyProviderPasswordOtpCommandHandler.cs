using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Contracts.Auth.Password;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Dto.PasswordRecovery;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.MarineProvider.Application.Auth.Password.VerifyProviderPasswordOtp;

/// <summary>
/// Delegates OTP verification to Identity via service-token remote call.
/// Maps the Identity response into the existing BFF response contract (frontend unchanged).
/// </summary>
public sealed class VerifyProviderPasswordOtpCommandHandler
    : AizenCommandHandler<VerifyProviderPasswordOtpCommand, VerifyProviderPasswordOtpResponse>
{
    private readonly IProviderIdentityRemoteCall _identity;
    private readonly ILogger<VerifyProviderPasswordOtpCommandHandler> _logger;

    public VerifyProviderPasswordOtpCommandHandler(
        IProviderIdentityRemoteCall identity,
        ILogger<VerifyProviderPasswordOtpCommandHandler> logger)
    {
        _identity = identity;
        _logger = logger;
    }

    public override async Task<VerifyProviderPasswordOtpResponse?> Handle(
        VerifyProviderPasswordOtpCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _identity.VerifyProviderPasswordRecoveryOtp(
                new VerifyProviderPasswordRecoveryOtpRequest
                {
                    ResetRequestId = request.ResetRequestId,
                    OtpCode = request.OtpCode,
                });

            var data = result.Body;
            if (data is not null)
            {
                return new VerifyProviderPasswordOtpResponse
                {
                    Verified = data.Verified,
                    ResetToken = data.ResetToken,
                    ExpiresInSeconds = data.ExpiresInSeconds,
                    Message = data.Message,
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Identity OTP verification failed.");
        }

        return new VerifyProviderPasswordOtpResponse
        {
            Verified = false,
            Message = "The code is invalid or has expired. Please request a new code.",
        };
    }
}
