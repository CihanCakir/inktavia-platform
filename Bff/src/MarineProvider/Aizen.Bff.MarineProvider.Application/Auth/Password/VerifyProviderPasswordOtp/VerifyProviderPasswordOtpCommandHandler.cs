using Aizen.Bff.MarineProvider.Application.Common.Options;
using Aizen.Bff.MarineProvider.Application.Common.Services.PasswordRecovery;
using Aizen.Bff.MarineProvider.Application.Contracts.Auth.Password;
using Aizen.Core.CQRS.Handler;
using Microsoft.Extensions.Options;

namespace Aizen.Bff.MarineProvider.Application.Auth.Password.VerifyProviderPasswordOtp;

/// <summary>
/// Verifies the OTP against the stored hash with attempt limiting + TTL. On success it consumes the OTP and issues
/// a short-lived, single-use reset token of the form "{resetRequestId}.{secret}" (only the secret hash is stored).
/// No login session or Keycloak access token is ever created here.
/// </summary>
public sealed class VerifyProviderPasswordOtpCommandHandler
    : AizenCommandHandler<VerifyProviderPasswordOtpCommand, VerifyProviderPasswordOtpResponse>
{
    private readonly IProviderPasswordRecoveryStore _store;
    private readonly PasswordRecoveryOptions _options;

    public VerifyProviderPasswordOtpCommandHandler(
        IProviderPasswordRecoveryStore store,
        IOptions<PasswordRecoveryOptions> options)
    {
        _store = store;
        _options = options.Value;
    }

    public override async Task<VerifyProviderPasswordOtpResponse?> Handle(
        VerifyProviderPasswordOtpCommand request, CancellationToken cancellationToken)
    {
        var invalid = new VerifyProviderPasswordOtpResponse
        {
            Verified = false,
            Message = "The code is invalid or has expired. Please request a new code.",
        };

        var record = await _store.GetAsync(request.ResetRequestId, cancellationToken);
        if (record is null) return invalid;

        var now = DateTime.UtcNow;

        if (record.ConsumedAtUtc is not null || record.OtpExpiresAtUtc <= now)
        {
            await _store.RemoveAsync(request.ResetRequestId, cancellationToken);
            return invalid;
        }

        if (record.Attempts >= record.MaxAttempts)
        {
            await _store.RemoveAsync(request.ResetRequestId, cancellationToken);
            return invalid;
        }

        if (!PasswordRecoverySecurity.Verify(request.OtpCode, record.OtpHash, record.OtpSalt))
        {
            record.Attempts++;
            var remainingTtl = TimeSpan.FromSeconds(Math.Max(1, (int)(record.OtpExpiresAtUtc - now).TotalSeconds));
            await _store.SaveAsync(record, remainingTtl, cancellationToken);
            return invalid;
        }

        // Success — consume the OTP and mint a single-use reset token.
        var secret = PasswordRecoverySecurity.GenerateOpaqueToken();
        var (tokenHash, tokenSalt) = PasswordRecoverySecurity.Hash(secret);

        record.ConsumedAtUtc = now;
        record.ResetTokenHash = tokenHash;
        record.ResetTokenSalt = tokenSalt;
        record.ResetTokenExpiresAtUtc = now.AddSeconds(_options.ResetTokenTtlSeconds);

        await _store.SaveAsync(record, TimeSpan.FromSeconds(_options.ResetTokenTtlSeconds + 30), cancellationToken);

        return new VerifyProviderPasswordOtpResponse
        {
            Verified = true,
            ResetToken = $"{record.ResetRequestId}.{secret}",
            ExpiresInSeconds = _options.ResetTokenTtlSeconds,
            Message = "Code verified. You may now set a new password.",
        };
    }
}
