using Aizen.Bff.MarineProvider.Application.Common;
using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Contracts.Auth.Password;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Identity.Abstraction.Dto.PasswordRecovery;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.MarineProvider.Application.Auth;

/// <summary>
/// Delegates OTP verification to Identity via service-token remote call.
/// FAZ13A #67: geçersiz/expired OTP artık 200 + { Verified=false } olarak GİZLENMİYOR — AizenBusinessException
/// olarak fırlatılıyor → düzgün 400 failure envelope. Anti-enumerasyon-hassas uçlar (Forgot/Request/Resend)
/// AYRI ve kapsam DIŞI; OTP doğrulama (oturumlu) hesap varlığını açığa çıkarmaz. BACKEND BORCU: modül kodsuz
/// { Verified=false } döndürüyor; mesaj kodsuz taşınıyor (frontend backend metnine düşer). Modül OtpWrong/
/// OtpExpired gibi kararlı kod fırlatmalı.
/// </summary>
public sealed class VerifyProviderPasswordOtpCommandHandler
    : AizenCommandHandler<VerifyProviderPasswordOtpCommand, VerifyProviderPasswordOtpResponse>
{
    private readonly IIdentityRemoteCall _identity;
    private readonly ILogger<VerifyProviderPasswordOtpCommandHandler> _logger;

    public VerifyProviderPasswordOtpCommandHandler(
        IIdentityRemoteCall identity,
        ILogger<VerifyProviderPasswordOtpCommandHandler> logger)
    {
        _identity = identity;
        _logger = logger;
    }

    public override async Task<VerifyProviderPasswordOtpResponse?> Handle(
        VerifyProviderPasswordOtpCommand request, CancellationToken cancellationToken)
    {
        VerifyProviderPasswordRecoveryOtpResponse? data;
        try
        {
            var result = await _identity.VerifyProviderPasswordRecoveryOtp(
                new VerifyProviderPasswordRecoveryOtpRequest
                {
                    ResetRequestId = request.ResetRequestId,
                    OtpCode = request.OtpCode,
                });
            data = result.Body;
        }
        catch (Refit.ApiException ex)
        {
            _logger.LogWarning(ex, "Identity OTP verification rejected.");
            throw ModuleFailurePropagation.FromApiException(ex,
                "The code is invalid or has expired. Please request a new code.");
        }

        if (data is null || !data.Verified)
            throw new AizenBusinessException(
                data?.Message ?? "The code is invalid or has expired. Please request a new code.");

        return new VerifyProviderPasswordOtpResponse
        {
            Verified = true,
            ResetToken = data.ResetToken,
            ExpiresInSeconds = data.ExpiresInSeconds,
            Message = data.Message,
        };
    }
}
