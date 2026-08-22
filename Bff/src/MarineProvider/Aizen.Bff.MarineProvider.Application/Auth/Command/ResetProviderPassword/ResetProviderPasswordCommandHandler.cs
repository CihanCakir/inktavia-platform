using Aizen.Bff.MarineProvider.Application.Common;
using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Identity.Abstraction.Dto.PasswordRecovery;
using Microsoft.Extensions.Logging;
using BffResetResponse = Aizen.Bff.MarineProvider.Application.Contracts.Auth.Password.ResetProviderPasswordResponse;

namespace Aizen.Bff.MarineProvider.Application.Auth;

/// <summary>
/// Delegates password reset to Identity via service-token remote call.
/// FAZ13A #67: başarısızlık artık 200 + { Success=false } olarak GİZLENMİYOR — AizenBusinessException olarak
/// fırlatılıyor → düzgün 400 failure envelope. NOT: anti-enumerasyon-hassas uçlar (Forgot/Request/Resend)
/// AYRI ve kapsam DIŞI; onlar bilerek uniform 200 döner. Reset (token'lı) hesap varlığını açığa çıkarmaz.
/// BACKEND BORCU: Identity parola-kurtarma domain servisi kodsuz { Success=false } döndürüyor; mesaj kodsuz
/// taşınıyor (frontend backend metnine düşer). Modül kararlı kod (ör. OtpExpired) fırlatmalı.
/// </summary>
public sealed class ResetProviderPasswordCommandHandler
    : AizenCommandHandler<ResetProviderPasswordCommand, BffResetResponse>
{
    private readonly IIdentityRemoteCall _identity;
    private readonly ILogger<ResetProviderPasswordCommandHandler> _logger;

    public ResetProviderPasswordCommandHandler(
        IIdentityRemoteCall identity,
        ILogger<ResetProviderPasswordCommandHandler> logger)
    {
        _identity = identity;
        _logger = logger;
    }

    public override async Task<BffResetResponse?> Handle(
        ResetProviderPasswordCommand request, CancellationToken cancellationToken)
    {
        ResetProviderPasswordResponse? data;
        try
        {
            var result = await _identity.ResetProviderPassword(
                new ResetProviderPasswordRequest
                {
                    ResetToken = request.ResetToken,
                    NewPassword = request.NewPassword,
                    ConfirmPassword = request.ConfirmPassword,
                });
            data = result.Body;
        }
        catch (Refit.ApiException ex)
        {
            _logger.LogWarning(ex, "Identity password reset rejected.");
            throw ModuleFailurePropagation.FromApiException(ex,
                "Your reset session has expired. Please restart the password recovery.");
        }

        if (data is null || !data.Success)
            throw new AizenBusinessException(
                data?.Message ?? "Your reset session has expired. Please restart the password recovery.");

        return new BffResetResponse { Success = true, Message = data.Message };
    }
}
