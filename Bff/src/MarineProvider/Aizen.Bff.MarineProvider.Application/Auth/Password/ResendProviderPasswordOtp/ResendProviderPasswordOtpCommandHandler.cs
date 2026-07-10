using Aizen.Bff.MarineProvider.Application.Common.Options;
using Aizen.Bff.MarineProvider.Application.Common.Services.PasswordRecovery;
using Aizen.Bff.MarineProvider.Application.Contracts.Auth.Password;
using Aizen.Core.CQRS.Handler;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aizen.Bff.MarineProvider.Application.Auth.Password.ResendProviderPasswordOtp;

/// <summary>
/// Re-issues an OTP for an in-flight request, enforcing the resend cooldown. Always returns a generic response so
/// it cannot be used to probe account existence. A new OTP replaces the previous one and resets the attempt count.
/// </summary>
public sealed class ResendProviderPasswordOtpCommandHandler
    : AizenCommandHandler<ResendProviderPasswordOtpCommand, ResendProviderPasswordOtpResponse>
{
    private readonly IProviderPasswordRecoveryStore _store;
    private readonly IProviderPasswordRecoveryNotifier _notifier;
    private readonly PasswordRecoveryOptions _options;
    private readonly ILogger<ResendProviderPasswordOtpCommandHandler> _logger;

    public ResendProviderPasswordOtpCommandHandler(
        IProviderPasswordRecoveryStore store,
        IProviderPasswordRecoveryNotifier notifier,
        IOptions<PasswordRecoveryOptions> options,
        ILogger<ResendProviderPasswordOtpCommandHandler> logger)
    {
        _store = store;
        _notifier = notifier;
        _options = options.Value;
        _logger = logger;
    }

    public override async Task<ResendProviderPasswordOtpResponse?> Handle(
        ResendProviderPasswordOtpCommand request, CancellationToken cancellationToken)
    {
        var response = new ResendProviderPasswordOtpResponse
        {
            Resent = true,
            ResendAfterSeconds = _options.ResendCooldownSeconds,
            ExpiresInSeconds = _options.OtpTtlSeconds,
            Message = "If an account exists, a new verification code has been sent.",
        };

        var record = await _store.GetAsync(request.ResetRequestId, cancellationToken);
        if (record is null || record.ConsumedAtUtc is not null) return response;

        var now = DateTime.UtcNow;
        var elapsed = (now - record.LastSentAtUtc).TotalSeconds;
        if (elapsed < _options.ResendCooldownSeconds)
        {
            // Still cooling down — report remaining time, do not send a new code.
            response.ResendAfterSeconds = (int)Math.Ceiling(_options.ResendCooldownSeconds - elapsed);
            return response;
        }

        var otp = PasswordRecoverySecurity.GenerateNumericOtp(_options.OtpLength);
        var (otpHash, otpSalt) = PasswordRecoverySecurity.Hash(otp);

        record.OtpHash = otpHash;
        record.OtpSalt = otpSalt;
        record.OtpExpiresAtUtc = now.AddSeconds(_options.OtpTtlSeconds);
        record.Attempts = 0;
        record.LastSentAtUtc = now;

        var ttl = TimeSpan.FromSeconds(Math.Max(_options.OtpTtlSeconds, _options.ResetTokenTtlSeconds) + 30);
        await _store.SaveAsync(record, ttl, cancellationToken);

        try
        {
            await _notifier.SendOtpAsync(record.Channel, record.MaskedTarget, otp, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Password recovery: OTP resend dispatch failed for {MaskedTarget}.", record.MaskedTarget);
        }

        return response;
    }
}
