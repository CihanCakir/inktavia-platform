using System.Security.Cryptography;
using System.Text;
using Aizen.Core.Cache.Abstraction;
using Aizen.Modules.Identity.Abstraction;
using Aizen.Modules.Identity.Domain.Entities;
using Aizen.Modules.Identity.Domain.Entities.PasswordRecovery;
using Aizen.Modules.Identity.Domain.Interface;
using Aizen.Modules.Identity.Domain.Interface.Service;
using Aizen.Modules.Identity.Domain.Model.PasswordRecovery;
using Aizen.Modules.Identity.Repository.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aizen.Modules.Identity.Repository.Identity.Service.PasswordRecovery;

public sealed class ProviderPasswordRecoveryDomainService : IProviderPasswordRecoveryDomainService
{
    private readonly IdentityDbContext _db;
    private readonly UserManager<UserEntity> _userManager;
    private readonly IUserProfileRepository _profileRepo;
    private readonly IIdentityKeycloakPasswordService _keycloakPassword;
    private readonly IProviderPasswordRecoveryNotifier _notifier;
    private readonly IAizenDistributedCache _cache;
    private readonly PasswordRecoveryOptions _options;
    private readonly ILogger<ProviderPasswordRecoveryDomainService> _logger;

    private const string RateLimitKeyPrefix = "provider:pwreset:rl:";

    public ProviderPasswordRecoveryDomainService(
        IdentityDbContext db,
        UserManager<UserEntity> userManager,
        IUserProfileRepository profileRepo,
        IIdentityKeycloakPasswordService keycloakPassword,
        IProviderPasswordRecoveryNotifier notifier,
        IAizenDistributedCache cache,
        IOptions<PasswordRecoveryOptions> options,
        ILogger<ProviderPasswordRecoveryDomainService> logger)
    {
        _db = db;
        _userManager = userManager;
        _profileRepo = profileRepo;
        _keycloakPassword = keycloakPassword;
        _notifier = notifier;
        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<PasswordRecoveryRequestResult> RequestAsync(string channel, string identifier, CancellationToken ct)
    {
        channel = channel.Trim().ToLowerInvariant();
        identifier = identifier.Trim();

        var syntheticResult = new PasswordRecoveryRequestResult
        {
            ResetRequestId = PasswordRecoverySecurity.GenerateOpaqueToken(),
            MaskedTarget = channel == "email"
                ? PasswordRecoverySecurity.MaskEmail(identifier.ToLowerInvariant())
                : PasswordRecoverySecurity.MaskPhone(identifier),
            OtpLength = _options.OtpLength,
            ExpiresInSeconds = _options.OtpTtlSeconds,
            ResendAfterSeconds = _options.ResendCooldownSeconds,
        };

        // Per-identifier rate limit (non-enumerating: exceeding returns the same generic response)
        if (await IsIdentifierThrottledAsync(identifier))
        {
            _logger.LogWarning("Password recovery: per-identifier rate limit exceeded for {Channel}.", channel);
            return syntheticResult;
        }

        // Resolve the user
        UserEntity? user = null;
        try
        {
            if (channel == "email")
            {
                user = await _userManager.FindByEmailAsync(identifier.ToLowerInvariant());
            }
            else if (channel == "phone")
            {
                var normalized = NormalizePhone(identifier);
                if (normalized is not null)
                {
                    user = await _userManager.Users
                        .FirstOrDefaultAsync(u => u.PhoneNumber == normalized, ct);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Password recovery: user lookup failed (channel {Channel}).", channel);
        }

        if (user is null || string.IsNullOrWhiteSpace(user.KeycloakSubjectId))
            return syntheticResult;

        // Require an Organizer profile
        var profile = await _profileRepo.GetActiveProfileIdAsync(user.Id, WorkshopRoleContext.Organizer);
        if (profile is null)
            return syntheticResult;

        var otp = PasswordRecoverySecurity.GenerateNumericOtp(_options.OtpLength);
        var (otpHash, otpSalt) = PasswordRecoverySecurity.Hash(otp);
        var (targetHash, _) = PasswordRecoverySecurity.Hash(identifier.ToLowerInvariant());

        var entity = ProviderPasswordRecoveryRequestEntity.Create(
            resetRequestId: syntheticResult.ResetRequestId,
            keycloakSubjectId: user.KeycloakSubjectId,
            userId: user.Id,
            providerProfileId: profile.Id,
            channel: channel,
            targetHash: targetHash,
            maskedTarget: syntheticResult.MaskedTarget,
            otpHash: otpHash,
            otpSalt: otpSalt,
            otpExpiresAtUtc: DateTime.UtcNow.AddSeconds(_options.OtpTtlSeconds),
            maxAttempts: _options.MaxAttempts);

        _db.ProviderPasswordRecoveryRequests.Add(entity);
        await _db.SaveChangesAsync(ct);

        try
        {
            await _notifier.SendOtpAsync(
                channel, syntheticResult.MaskedTarget, otp,
                recipientUserId: user.Id,
                email: channel == "email" ? identifier.ToLowerInvariant() : null,
                phone: channel == "phone" ? identifier : null,
                expiresInMinutes: (int)Math.Ceiling(_options.OtpTtlSeconds / 60.0),
                ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Password recovery: OTP dispatch failed for {MaskedTarget}.", syntheticResult.MaskedTarget);
        }

        return syntheticResult;
    }

    public async Task<PasswordRecoveryVerifyResult> VerifyOtpAsync(string resetRequestId, string otpCode, CancellationToken ct)
    {
        var invalid = new PasswordRecoveryVerifyResult
        {
            Verified = false,
            Message = "The code is invalid or has expired. Please request a new code.",
        };

        var record = await _db.ProviderPasswordRecoveryRequests
            .FirstOrDefaultAsync(r => r.ResetRequestId == resetRequestId, ct);

        if (record is null) return invalid;

        var now = DateTime.UtcNow;

        if (record.ConsumedAtUtc is not null || record.IsOtpExpired())
        {
            _db.ProviderPasswordRecoveryRequests.Remove(record);
            await _db.SaveChangesAsync(ct);
            return invalid;
        }

        if (record.Attempts >= record.MaxAttempts)
        {
            _db.ProviderPasswordRecoveryRequests.Remove(record);
            await _db.SaveChangesAsync(ct);
            return invalid;
        }

        if (!PasswordRecoverySecurity.Verify(otpCode, record.OtpHash, record.OtpSalt))
        {
            record.IncrementAttempt();
            await _db.SaveChangesAsync(ct);
            return invalid;
        }

        // Success — consume the OTP and mint a single-use reset token.
        var secret = PasswordRecoverySecurity.GenerateOpaqueToken();
        var (tokenHash, tokenSalt) = PasswordRecoverySecurity.Hash(secret);

        record.MarkConsumed();
        record.SetResetToken(tokenHash, tokenSalt, now.AddSeconds(_options.ResetTokenTtlSeconds));
        await _db.SaveChangesAsync(ct);

        return new PasswordRecoveryVerifyResult
        {
            Verified = true,
            ResetToken = $"{record.ResetRequestId}.{secret}",
            ExpiresInSeconds = _options.ResetTokenTtlSeconds,
        };
    }

    public async Task<PasswordRecoveryResetResult> ResetAsync(string resetToken, string newPassword, CancellationToken ct)
    {
        var expired = new PasswordRecoveryResetResult
        {
            Success = false,
            Message = "Your reset session has expired. Please restart the password recovery.",
        };

        var separator = resetToken.IndexOf('.');
        if (separator <= 0 || separator >= resetToken.Length - 1) return expired;

        var resetRequestId = resetToken[..separator];
        var secret = resetToken[(separator + 1)..];

        var record = await _db.ProviderPasswordRecoveryRequests
            .FirstOrDefaultAsync(r => r.ResetRequestId == resetRequestId, ct);

        if (record?.ResetTokenHash is null || record.ResetTokenSalt is null) return expired;

        if (record.ResetTokenExpiresAtUtc is null || record.ResetTokenExpiresAtUtc <= DateTime.UtcNow)
        {
            _db.ProviderPasswordRecoveryRequests.Remove(record);
            await _db.SaveChangesAsync(ct);
            return expired;
        }

        if (!PasswordRecoverySecurity.Verify(secret, record.ResetTokenHash, record.ResetTokenSalt))
            return expired;

        try
        {
            await _keycloakPassword.ResetPasswordAsync(record.KeycloakSubjectId, newPassword, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Keycloak password reset failed for user {KeycloakSubjectId}.", record.KeycloakSubjectId);
            return new PasswordRecoveryResetResult
            {
                Success = false,
                Message = "Your password could not be reset. Please try again.",
            };
        }

        // Best-effort session revocation
        try
        {
            await _keycloakPassword.RevokeSessionsAsync(record.KeycloakSubjectId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Keycloak session revocation failed after reset for user {KeycloakSubjectId}.", record.KeycloakSubjectId);
        }

        _db.ProviderPasswordRecoveryRequests.Remove(record);
        await _db.SaveChangesAsync(ct);

        return new PasswordRecoveryResetResult
        {
            Success = true,
            Message = "Your password has been reset. Please sign in again.",
        };
    }

    public async Task<PasswordRecoveryResendResult> ResendAsync(string resetRequestId, CancellationToken ct)
    {
        var response = new PasswordRecoveryResendResult
        {
            Resent = true,
            ResendAfterSeconds = _options.ResendCooldownSeconds,
            ExpiresInSeconds = _options.OtpTtlSeconds,
        };

        var record = await _db.ProviderPasswordRecoveryRequests
            .FirstOrDefaultAsync(r => r.ResetRequestId == resetRequestId, ct);

        if (record is null || record.ConsumedAtUtc is not null) return response;

        var now = DateTime.UtcNow;
        var elapsed = (now - record.LastSentAtUtc).TotalSeconds;
        if (elapsed < _options.ResendCooldownSeconds)
        {
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

        await _db.SaveChangesAsync(ct);

        // Resolve the user's contact info for delivery
        string? email = null;
        string? phone = null;
        var user = await _userManager.FindByIdAsync(record.UserId.ToString());
        if (user is not null)
        {
            email = record.Channel == "email" ? user.Email : null;
            phone = record.Channel == "phone" ? user.PhoneNumber : null;
        }

        try
        {
            await _notifier.SendOtpAsync(
                record.Channel, record.MaskedTarget, otp,
                recipientUserId: record.UserId,
                email: email,
                phone: phone,
                expiresInMinutes: (int)Math.Ceiling(_options.OtpTtlSeconds / 60.0),
                ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Password recovery: OTP resend dispatch failed for {MaskedTarget}.", record.MaskedTarget);
        }

        return response;
    }

    /// <summary>
    /// Per-identifier rate limit via Redis. Returns true if the identifier has exceeded the allowed
    /// request count within the configured window. Fails open (returns false) on cache errors.
    /// The key is a SHA256 hash of the normalized identifier — never the raw email/phone.
    /// </summary>
    private async Task<bool> IsIdentifierThrottledAsync(string identifier)
    {
        if (_options.MaxRequestsPerIdentifierPerWindow <= 0)
            return false;

        var keyHash = Sha256Hex(identifier.ToLowerInvariant());
        var key = $"{RateLimitKeyPrefix}{keyHash}";
        var window = TimeSpan.FromSeconds(_options.IdentifierWindowSeconds);

        try
        {
            var current = await _cache.GetNoHash<int>(key);
            if (current >= _options.MaxRequestsPerIdentifierPerWindow)
                return true;

            await _cache.SetNoHash(key, current + 1, window);
            return false;
        }
        catch (Exception ex)
        {
            // Fail open — do not block legitimate resets on a cache outage
            _logger.LogWarning(ex, "Password recovery: per-identifier rate limit cache error; failing open.");
            return false;
        }
    }

    private static string Sha256Hex(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string? NormalizePhone(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;
        var digits = new string(input.Where(char.IsDigit).ToArray());
        if (digits.StartsWith("0")) digits = digits[1..];
        if (!digits.StartsWith("90")) digits = "90" + digits;
        return "+" + digits;
    }
}
