using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Contracts.Auth.Password;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Dto.PasswordRecovery;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.MarineProvider.Application.Auth;

/// <summary>
/// Delegates password recovery request to the Identity module via service-token remote call.
/// Maps the Identity response into the existing BFF response contract (frontend unchanged).
/// </summary>
public sealed class ForgotProviderPasswordCommandHandler
    : AizenCommandHandler<ForgotProviderPasswordCommand, ForgotProviderPasswordResponse>
{
    private readonly IIdentityRemoteCall _identity;
    private readonly ILogger<ForgotProviderPasswordCommandHandler> _logger;

    public ForgotProviderPasswordCommandHandler(
        IIdentityRemoteCall identity,
        ILogger<ForgotProviderPasswordCommandHandler> logger)
    {
        _identity = identity;
        _logger = logger;
    }

    public override async Task<ForgotProviderPasswordResponse?> Handle(
        ForgotProviderPasswordCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _identity.RequestProviderPasswordRecovery(
                new RequestProviderPasswordRecoveryRequest
                {
                    Channel = request.Channel,
                    Identifier = request.Identifier,
                });

            var data = result.Body;
            if (data is not null)
            {
                return new ForgotProviderPasswordResponse
                {
                    Accepted = data.Accepted,
                    ResetRequestId = data.ResetRequestId,
                    MaskedTarget = data.MaskedTarget,
                    OtpLength = data.OtpLength,
                    ExpiresInSeconds = data.ExpiresInSeconds,
                    ResendAfterSeconds = data.ResendAfterSeconds,
                    Message = data.Message,
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Identity password recovery request failed.");
            return new ForgotProviderPasswordResponse
            {
                Accepted = false,
                Message = "Service temporarily unavailable. Please try again later.",
            };
        }

        // Identity returned a null body — treat as unknown account (anti-enumeration).
        return new ForgotProviderPasswordResponse
        {
            Accepted = true,
            Message = "If an account exists, a verification code has been sent.",
        };
    }
}
