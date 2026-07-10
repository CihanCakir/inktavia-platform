using Aizen.Bff.MarineProvider.Application.Common.Options;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Bff.MarineProvider.Application.Common.Services.PasswordRecovery;
using Aizen.Bff.MarineProvider.Application.Contracts.Auth.Password;
using Aizen.Core.CQRS.Handler;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aizen.Bff.MarineProvider.Application.Auth.Password.ForgotProviderPassword;

/// <summary>
/// Orchestrates a password recovery request. Always returns a generic accepted response so the endpoint never
/// reveals whether an account exists. When the account is resolvable (email channel via Keycloak), a 6-digit OTP
/// is generated, hashed, stored in the distributed cache, and dispatched via the notifier.
/// </summary>
public sealed class ForgotProviderPasswordCommandHandler
    : AizenCommandHandler<ForgotProviderPasswordCommand, ForgotProviderPasswordResponse>
{
    private readonly IProviderKeycloakAdminClient _keycloak;
    private readonly IProviderPasswordRecoveryStore _store;
    private readonly IProviderPasswordRecoveryNotifier _notifier;
    private readonly PasswordRecoveryOptions _options;
    private readonly ILogger<ForgotProviderPasswordCommandHandler> _logger;

    public ForgotProviderPasswordCommandHandler(
        IProviderKeycloakAdminClient keycloak,
        IProviderPasswordRecoveryStore store,
        IProviderPasswordRecoveryNotifier notifier,
        IOptions<PasswordRecoveryOptions> options,
        ILogger<ForgotProviderPasswordCommandHandler> logger)
    {
        _keycloak = keycloak;
        _store = store;
        _notifier = notifier;
        _options = options.Value;
        _logger = logger;
    }

    public override async Task<ForgotProviderPasswordResponse?> Handle(
        ForgotProviderPasswordCommand request, CancellationToken cancellationToken)
    {
        var channel = request.Channel.Trim().ToLowerInvariant();
        var identifier = request.Identifier.Trim();

        // Generic response scaffold — identical shape whether or not an account exists.
        var response = new ForgotProviderPasswordResponse
        {
            Accepted = true,
            ResetRequestId = PasswordRecoverySecurity.GenerateOpaqueToken(),
            MaskedTarget = channel == "email"
                ? PasswordRecoverySecurity.MaskEmail(identifier.ToLowerInvariant())
                : PasswordRecoverySecurity.MaskPhone(identifier),
            OtpLength = _options.OtpLength,
            ExpiresInSeconds = _options.OtpTtlSeconds,
            ResendAfterSeconds = _options.ResendCooldownSeconds,
            Message = "If an account exists, a verification code has been sent.",
        };

        // Resolve the target Keycloak user. Email is resolvable today; phone requires an Identity
        // find-provider-by-phone lookup that is not yet available (planned backend dependency).
        KeycloakUserRef? user = null;
        try
        {
            if (channel == "email")
                user = await _keycloak.FindUserByEmailAsync(identifier.ToLowerInvariant(), cancellationToken);
            // else: phone channel — planned; leaves user null → generic response, no OTP generated.
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Password recovery: user lookup failed (channel {Channel}).", channel);
        }

        if (user is null)
        {
            // Do not persist a record; a later verify against this synthetic id fails as invalid/expired.
            return response;
        }

        var otp = PasswordRecoverySecurity.GenerateNumericOtp(_options.OtpLength);
        var (otpHash, otpSalt) = PasswordRecoverySecurity.Hash(otp);
        var (targetHash, _) = PasswordRecoverySecurity.Hash(identifier.ToLowerInvariant());
        var now = DateTime.UtcNow;

        var record = new PasswordRecoveryRecord
        {
            ResetRequestId = response.ResetRequestId,
            KeycloakUserId = user.Id,
            Channel = channel,
            TargetHash = targetHash,
            MaskedTarget = response.MaskedTarget,
            OtpHash = otpHash,
            OtpSalt = otpSalt,
            OtpExpiresAtUtc = now.AddSeconds(_options.OtpTtlSeconds),
            Attempts = 0,
            MaxAttempts = _options.MaxAttempts,
            CreatedAtUtc = now,
            LastSentAtUtc = now,
        };

        var ttl = TimeSpan.FromSeconds(Math.Max(_options.OtpTtlSeconds, _options.ResetTokenTtlSeconds) + 30);
        await _store.SaveAsync(record, ttl, cancellationToken);

        try
        {
            await _notifier.SendOtpAsync(channel, response.MaskedTarget, otp, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Password recovery: OTP dispatch failed for {MaskedTarget}.", response.MaskedTarget);
        }

        return response;
    }
}
