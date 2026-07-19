using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Dto.PasswordRecovery;
using Microsoft.Extensions.Logging;
using BffResetResponse = Aizen.Bff.MarineProvider.Application.Contracts.Auth.Password.ResetProviderPasswordResponse;

namespace Aizen.Bff.MarineProvider.Application.Auth;

/// <summary>
/// Delegates password reset to Identity via service-token remote call.
/// Maps the Identity response into the existing BFF response contract (frontend unchanged).
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
        try
        {
            var result = await _identity.ResetProviderPassword(
                new ResetProviderPasswordRequest
                {
                    ResetToken = request.ResetToken,
                    NewPassword = request.NewPassword,
                    ConfirmPassword = request.ConfirmPassword,
                });

            var data = result.Body;
            if (data is not null)
            {
                return new BffResetResponse
                {
                    Success = data.Success,
                    Message = data.Message,
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Identity password reset failed.");
        }

        return new BffResetResponse
        {
            Success = false,
            Message = "Your reset session has expired. Please restart the password recovery.",
        };
    }
}
