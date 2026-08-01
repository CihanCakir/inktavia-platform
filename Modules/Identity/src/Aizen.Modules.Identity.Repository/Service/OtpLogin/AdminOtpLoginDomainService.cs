using System.Security.Cryptography;
using System.Text;
using Aizen.Core.Cache.Abstraction;
using Aizen.Modules.Identity.Abstraction;
using Aizen.Modules.Identity.Domain.Entities;
using Aizen.Modules.Identity.Domain.Entities.OtpLogin;
using Aizen.Modules.Identity.Domain.Interface;
using Aizen.Modules.Identity.Domain.Interface.Service;
using Aizen.Modules.Identity.Domain.Model.OtpLogin;
using Aizen.Modules.Identity.Repository.Context;
using Aizen.Modules.Identity.Repository.Identity.Service.PasswordRecovery;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aizen.Modules.Identity.Repository.Identity.Service.OtpLogin;

public sealed class AdminOtpLoginDomainService : IAdminOtpLoginDomainService
{
    private readonly IdentityDbContext _db;
    private readonly UserManager<UserEntity> _userManager;
    private readonly IUserProfileRepository _profileRepo;
    private readonly IProviderOtpLoginNotifier _notifier;
    private readonly IAizenDistributedCache _cache;
    private readonly OtpLoginOptions _options;
    private readonly IProviderOtpLoginTicketService _ticketService;
    private readonly ILogger<AdminOtpLoginDomainService> _logger;

    private const string RateLimitKeyPrefix = "admin:otplogin:rl:";

    public AdminOtpLoginDomainService(
        IdentityDbContext db, UserManager<UserEntity> userManager,
        IUserProfileRepository profileRepo, IProviderOtpLoginNotifier notifier,
        IAizenDistributedCache cache, IOptions<OtpLoginOptions> options,
        IProviderOtpLoginTicketService ticketService,
        ILogger<AdminOtpLoginDomainService> logger)
    {
        _db = db; _userManager = userManager; _profileRepo = profileRepo;
        _notifier = notifier; _cache = cache; _options = options.Value;
        _ticketService = ticketService; _logger = logger;
    }

    public async Task<OtpLoginRequestResult> RequestAsync(string channel, string identifier, CancellationToken ct)
    {
        channel = channel.Trim().ToLowerInvariant();
        identifier = identifier.Trim();

        var syntheticResult = new OtpLoginRequestResult
        {
            LoginRequestId = PasswordRecoverySecurity.GenerateOpaqueToken(),
            MaskedTarget = channel == "email"
                ? PasswordRecoverySecurity.MaskEmail(identifier.ToLowerInvariant())
                : PasswordRecoverySecurity.MaskPhone(identifier),
            OtpLength = _options.OtpLength,
            ExpiresInSeconds = _options.OtpTtlSeconds,
            ResendAfterSeconds = _options.ResendCooldownSeconds,
        };

        if (await IsIdentifierThrottledAsync(identifier))
        {
            _logger.LogWarning("OTP login: per-identifier rate limit exceeded for {Channel}.", channel);
            return syntheticResult;
        }

        UserEntity? user = null;
        try
        {
            if (channel == "email")
                user = await _userManager.FindByEmailAsync(identifier.ToLowerInvariant());
            else if (channel == "phone")
            {
                var normalized = NormalizePhone(identifier);
                if (normalized is not null)
                    user = await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == normalized, ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OTP login: user lookup failed (channel {Channel}).", channel);
        }

        if (user is null || string.IsNullOrWhiteSpace(user.KeycloakSubjectId))
            return syntheticResult;

        // Admin gate: require the Admin role
        if (!await _userManager.IsInRoleAsync(user, Abstraction.Model.RoleNames.Admin))
            return syntheticResult;

        // Resolve admin profile (if present); admin role is the primary gate, profile is optional context
        var profile = await _profileRepo.GetActiveProfileIdAsync(user.Id, WorkshopRoleContext.Admin);
        var adminProfileId = profile?.Id;

        var otp = PasswordRecoverySecurity.GenerateNumericOtp(_options.OtpLength);
        var (otpHash, otpSalt) = PasswordRecoverySecurity.Hash(otp);
        var (targetHash, _) = PasswordRecoverySecurity.Hash(identifier.ToLowerInvariant());

        var entity = AdminOtpLoginRequestEntity.Create(
            loginRequestId: syntheticResult.LoginRequestId,
            keycloakSubjectId: user.KeycloakSubjectId,
            userId: user.Id, adminProfileId: adminProfileId,
            channel: channel, targetHash: targetHash, maskedTarget: syntheticResult.MaskedTarget,
            otpHash: otpHash, otpSalt: otpSalt,
            otpExpiresAtUtc: DateTime.UtcNow.AddSeconds(_options.OtpTtlSeconds),
            maxAttempts: _options.MaxAttempts);

        _db.AdminOtpLoginRequests.Add(entity);
        await _db.SaveChangesAsync(ct);

        try
        {
            await _notifier.SendOtpAsync(channel, syntheticResult.MaskedTarget, otp,
                recipientUserId: user.Id,
                email: channel == "email" ? identifier.ToLowerInvariant() : null,
                phone: channel == "phone" ? identifier : null,
                expiresInMinutes: (int)Math.Ceiling(_options.OtpTtlSeconds / 60.0), ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OTP login: dispatch failed for {MaskedTarget}.", syntheticResult.MaskedTarget);
        }

        return syntheticResult;
    }

    public async Task<OtpLoginVerifyResult> VerifyOtpAsync(string loginRequestId, string otpCode, CancellationToken ct)
    {
        var invalid = new OtpLoginVerifyResult
        {
            Verified = false,
            NextAction = "keycloak_handoff_required",
            Message = "The code is invalid or has expired. Please request a new code.",
        };

        var record = await _db.AdminOtpLoginRequests
            .FirstOrDefaultAsync(r => r.LoginRequestId == loginRequestId, ct);
        if (record is null) return invalid;

        if (record.ConsumedAtUtc is not null || record.IsOtpExpired())
        {
            _db.AdminOtpLoginRequests.Remove(record);
            await _db.SaveChangesAsync(ct);
            return invalid;
        }

        if (record.Attempts >= record.MaxAttempts)
        {
            _db.AdminOtpLoginRequests.Remove(record);
            await _db.SaveChangesAsync(ct);
            return invalid;
        }

        if (!PasswordRecoverySecurity.Verify(otpCode, record.OtpHash, record.OtpSalt))
        {
            record.IncrementAttempt();
            await _db.SaveChangesAsync(ct);
            return invalid;
        }

        record.MarkConsumed();
        await _db.SaveChangesAsync(ct);

        // Mint a single-use login ticket for Keycloak handoff
        try
        {
            var ticket = await _ticketService.MintAsync(record.KeycloakSubjectId, "admin-panel", ct);
            return new OtpLoginVerifyResult
            {
                Verified = true,
                NextAction = "redirect_to_keycloak_handoff",
                LoginTicket = ticket.LoginTicket,
                ExpiresInSeconds = ticket.ExpiresInSeconds,
                Message = "Code verified. Redirecting to complete sign-in.",
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OTP login: ticket mint failed for user {KeycloakSubjectId}.", record.KeycloakSubjectId);
            return new OtpLoginVerifyResult
            {
                Verified = true,
                NextAction = "keycloak_handoff_required",
                Message = "Code verified. Sign-in redirect is temporarily unavailable.",
            };
        }
    }

    public async Task<OtpLoginResendResult> ResendAsync(string loginRequestId, CancellationToken ct)
    {
        var response = new OtpLoginResendResult
        {
            Resent = true,
            ResendAfterSeconds = _options.ResendCooldownSeconds,
            ExpiresInSeconds = _options.OtpTtlSeconds,
        };

        var record = await _db.AdminOtpLoginRequests
            .FirstOrDefaultAsync(r => r.LoginRequestId == loginRequestId, ct);
        if (record is null || record.ConsumedAtUtc is not null) return response;

        var now = DateTime.UtcNow;
        var elapsed = (now - record.LastSentAtUtc).TotalSeconds;
        if (elapsed < _options.ResendCooldownSeconds)
        {
            response.Resent = false;
            response.ResendAfterSeconds = (int)Math.Ceiling(_options.ResendCooldownSeconds - elapsed);
            response.Message = "Please wait before requesting another code.";
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

        string? email = null; string? phone = null;
        var user = await _userManager.FindByIdAsync(record.UserId.ToString());
        if (user is not null)
        {
            email = record.Channel == "email" ? user.Email : null;
            phone = record.Channel == "phone" ? user.PhoneNumber : null;
        }

        try
        {
            await _notifier.SendOtpAsync(record.Channel, record.MaskedTarget, otp,
                recipientUserId: record.UserId, email: email, phone: phone,
                expiresInMinutes: (int)Math.Ceiling(_options.OtpTtlSeconds / 60.0), ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OTP login: resend dispatch failed for {MaskedTarget}.", record.MaskedTarget);
        }

        return response;
    }

    private async Task<bool> IsIdentifierThrottledAsync(string identifier)
    {
        if (_options.MaxRequestsPerIdentifierPerWindow <= 0) return false;
        var key = $"{RateLimitKeyPrefix}{Sha256Hex(identifier.ToLowerInvariant())}";
        var window = TimeSpan.FromSeconds(_options.IdentifierWindowSeconds);
        try
        {
            var current = await _cache.GetNoHash<int>(key);
            if (current >= _options.MaxRequestsPerIdentifierPerWindow) return true;
            await _cache.SetNoHash(key, current + 1, window);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OTP login: per-identifier rate limit cache error; failing open.");
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
