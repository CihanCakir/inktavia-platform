using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Bff.MarineProvider.Application.Common.Services.PasswordRecovery;
using Aizen.Bff.MarineProvider.Application.Contracts.Auth.Password;
using Aizen.Core.CQRS.Handler;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.MarineProvider.Application.Auth.Password.ResetProviderPassword;

/// <summary>
/// Validates the single-use reset token, then updates the provider's Keycloak password via the Admin API
/// (server-side, service-account authorized) and revokes existing Keycloak sessions. The reset request is deleted
/// on success so the token cannot be reused. No login token is returned — the user signs in again.
/// </summary>
public sealed class ResetProviderPasswordCommandHandler
    : AizenCommandHandler<ResetProviderPasswordCommand, ResetProviderPasswordResponse>
{
    private readonly IProviderPasswordRecoveryStore _store;
    private readonly IProviderKeycloakAdminClient _keycloak;
    private readonly ILogger<ResetProviderPasswordCommandHandler> _logger;

    public ResetProviderPasswordCommandHandler(
        IProviderPasswordRecoveryStore store,
        IProviderKeycloakAdminClient keycloak,
        ILogger<ResetProviderPasswordCommandHandler> logger)
    {
        _store = store;
        _keycloak = keycloak;
        _logger = logger;
    }

    public override async Task<ResetProviderPasswordResponse?> Handle(
        ResetProviderPasswordCommand request, CancellationToken cancellationToken)
    {
        var expired = new ResetProviderPasswordResponse
        {
            Success = false,
            Message = "Your reset session has expired. Please restart the password recovery.",
        };

        // Token format: "{resetRequestId}.{secret}"
        var separator = request.ResetToken.IndexOf('.');
        if (separator <= 0 || separator >= request.ResetToken.Length - 1) return expired;

        var resetRequestId = request.ResetToken[..separator];
        var secret = request.ResetToken[(separator + 1)..];

        var record = await _store.GetAsync(resetRequestId, cancellationToken);
        if (record?.ResetTokenHash is null || record.ResetTokenSalt is null) return expired;

        if (record.ResetTokenExpiresAtUtc is null || record.ResetTokenExpiresAtUtc <= DateTime.UtcNow)
        {
            await _store.RemoveAsync(resetRequestId, cancellationToken);
            return expired;
        }

        if (!PasswordRecoverySecurity.Verify(secret, record.ResetTokenHash, record.ResetTokenSalt))
            return expired;

        try
        {
            await _keycloak.ResetUserPasswordAsync(record.KeycloakUserId, request.NewPassword, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Keycloak password reset failed for user {UserId}.", record.KeycloakUserId);
            return new ResetProviderPasswordResponse
            {
                Success = false,
                Message = "Your password could not be reset. Please try again.",
            };
        }

        // Best-effort session revocation — a failure here does not fail the reset.
        try
        {
            await _keycloak.LogoutUserAsync(record.KeycloakUserId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Keycloak session revocation failed after reset for user {UserId}.", record.KeycloakUserId);
        }

        await _store.RemoveAsync(resetRequestId, cancellationToken);

        return new ResetProviderPasswordResponse
        {
            Success = true,
            Message = "Your password has been reset. Please sign in again.",
        };
    }
}
