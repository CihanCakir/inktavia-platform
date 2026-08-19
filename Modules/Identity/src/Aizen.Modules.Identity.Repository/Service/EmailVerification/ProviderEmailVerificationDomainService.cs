using System.Security.Cryptography;
using System.Text;
using Aizen.Core.Cache.Abstraction;
using Aizen.Modules.Identity.Domain.Entities;
using Aizen.Modules.Identity.Domain.Entities.EmailVerification;
using Aizen.Modules.Identity.Domain.Interface.Service;
using Aizen.Modules.Identity.Domain.Model.EmailVerification;
using Aizen.Modules.Identity.Repository.Context;
using Aizen.Modules.Identity.Repository.Identity.Service.PasswordRecovery;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aizen.Modules.Identity.Repository.Identity.Service.EmailVerification;

/// <summary>
/// Uygulama sahipliğindeki sağlayıcı e-posta doğrulama domain servisi. Parola kurtarmanın reset-token'ının
/// kardeşidir: opak <c>{Id}.{secret}</c> token, tuzlu PBKDF2 özeti (ham token ASLA saklanmaz), tek-kullanımlık,
/// sabit-zamanlı doğrulama. Kripto <see cref="PasswordRecoverySecurity"/>'den yeniden kullanılır.
///
/// SERT KISIT: Bu servis hiçbir oturum/çerez/JWT üretmez. Token yalnızca Keycloak emailVerified bayrağının
/// çevrilmesini yetkilendirir; doğrulama (<see cref="VerifyAsync"/>) ve tüketim (<see cref="ConsumeAsync"/>)
/// bilerek ayrıdır ki BFF önce doğrulasın, Keycloak'ı çevirsin, EN SON tüketsin.
///
/// Sağlayıcı kapsamı, ucun BFF servis-token'ıyla (IdentityWrite, yalnızca MarineProvider BFF) çağrılmasıyla
/// sağlanır; domain servisi kullanıcı + e-posta düzeyinde çalışır.
/// </summary>
public sealed class ProviderEmailVerificationDomainService : IProviderEmailVerificationDomainService
{
    private readonly IdentityDbContext _db;
    private readonly IProviderEmailVerificationNotifier _notifier;
    private readonly IAizenDistributedCache _cache;
    private readonly ProviderEmailVerificationOptions _options;
    private readonly ILogger<ProviderEmailVerificationDomainService> _logger;

    private const string ResendRateLimitKeyPrefix = "provider:emailverify:resend:rl:";
    private const string VerifyRateLimitKeyPrefix = "provider:emailverify:attempt:rl:";

    public ProviderEmailVerificationDomainService(
        IdentityDbContext db,
        IProviderEmailVerificationNotifier notifier,
        IAizenDistributedCache cache,
        IOptions<ProviderEmailVerificationOptions> options,
        ILogger<ProviderEmailVerificationDomainService> logger)
    {
        _db = db;
        _notifier = notifier;
        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<EmailVerificationGenerateResult> GenerateAsync(
        string keycloakSubjectId, string email, CancellationToken ct)
    {
        email = email.Trim().ToLowerInvariant();

        var result = new EmailVerificationGenerateResult
        {
            Accepted = true,
            MaskedTarget = PasswordRecoverySecurity.MaskEmail(email),
            ExpiresInSeconds = _options.TokenTtlSeconds,
            ResendAfterSeconds = _options.ResendCooldownSeconds,
        };

        UserEntity? user = null;
        try
        {
            if (!string.IsNullOrWhiteSpace(keycloakSubjectId))
                user = await _db.Users.FirstOrDefaultAsync(u => u.KeycloakSubjectId == keycloakSubjectId, ct);

            // Kayıt hemen provisioning sonrası çağrılır; subject henüz yazılmadıysa e-posta ile düş.
            if (user is null)
            {
                var normalized = email.ToUpperInvariant();
                user = await _db.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalized, ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Email verification: user lookup failed during generate.");
        }

        // Numaralandırma koruması: kullanıcı yoksa bile aynı genel yanıtı döneriz.
        if (user is null) return result;

        await IssueAndDispatchAsync(user, email, result.MaskedTarget, ct);
        return result;
    }

    public async Task<EmailVerificationVerifyResult> VerifyAsync(string token, CancellationToken ct)
    {
        var invalid = new EmailVerificationVerifyResult
        {
            Verified = false,
            Message = "Doğrulama bağlantısı geçersiz veya süresi dolmuş. Lütfen yeni bir bağlantı isteyin.",
        };

        if (!TryParseToken(token, out var id, out var secret)) return invalid;

        // Mütevazı hız sınırı: tek bir token tutamağının kısa pencerede denenme sayısını sınırla.
        if (await IsVerifyThrottledAsync(id))
        {
            _logger.LogWarning("Email verification: per-token verify rate limit exceeded.");
            return invalid;
        }

        var record = await _db.ProviderEmailVerifications.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (record is null) return invalid;

        if (!record.CanBeConsumed())
            return invalid;

        // Sabit-zamanlı doğrulama — yanlış token, doğru uzunlukta karşılaştırmayla sabit zamanda reddedilir.
        if (!PasswordRecoverySecurity.Verify(secret, record.TokenHash, record.TokenSalt))
            return invalid;

        // Başarı — TÜKETMEZ. BFF'in Keycloak'ta çevireceği kullanıcıyı döneriz.
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == record.UserId, ct);

        return new EmailVerificationVerifyResult
        {
            Verified = true,
            UserId = record.UserId,
            KeycloakSubjectId = user?.KeycloakSubjectId,
            Email = user?.Email,
            Message = "Bağlantı doğrulandı.",
        };
    }

    public async Task<EmailVerificationConsumeResult> ConsumeAsync(string token, CancellationToken ct)
    {
        var failed = new EmailVerificationConsumeResult
        {
            Consumed = false,
            Message = "Doğrulama bağlantısı geçersiz veya süresi dolmuş.",
        };

        if (!TryParseToken(token, out var id, out var secret)) return failed;

        if (await IsVerifyThrottledAsync(id))
        {
            _logger.LogWarning("Email verification: per-token consume rate limit exceeded.");
            return failed;
        }

        var record = await _db.ProviderEmailVerifications.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (record is null) return failed;

        // İkinci kez tüketim / süresi dolmuş → reddedilir (idempotent değil, bilerek: yakılmış token yeniden geçmez).
        if (!record.CanBeConsumed())
            return failed;

        if (!PasswordRecoverySecurity.Verify(secret, record.TokenHash, record.TokenSalt))
            return failed;

        record.MarkConsumed();
        await _db.SaveChangesAsync(ct);

        return new EmailVerificationConsumeResult
        {
            Consumed = true,
            Message = "E-posta doğrulandı.",
        };
    }

    public async Task<EmailVerificationResendResult> ResendAsync(string email, CancellationToken ct)
    {
        email = email.Trim().ToLowerInvariant();

        // Numaralandırma koruması: kullanıcı bulunsa da bulunmasa da aynı genel yanıt döner.
        var result = new EmailVerificationResendResult
        {
            Accepted = true,
            MaskedTarget = PasswordRecoverySecurity.MaskEmail(email),
            ResendAfterSeconds = _options.ResendCooldownSeconds,
            ExpiresInSeconds = _options.TokenTtlSeconds,
        };

        if (await IsIdentifierThrottledAsync(email))
        {
            _logger.LogWarning("Email verification: per-identifier resend rate limit exceeded.");
            return result;
        }

        UserEntity? user = null;
        try
        {
            var normalized = email.ToUpperInvariant();
            user = await _db.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalized, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Email verification: user lookup failed during resend.");
        }

        // Zaten doğrulanmışsa veya kullanıcı yoksa sessizce genel yanıt dön (bilgi sızdırma).
        if (user is null || user.EmailConfirmed) return result;

        // Bekleme süresi: en son bekleyen kaydın oluşturulmasından bu yana cooldown geçmediyse yeniden yollama.
        var latest = await _db.ProviderEmailVerifications
            .Where(r => r.UserId == user.Id && r.ConsumedAt == null)
            .OrderByDescending(r => r.Id)
            .FirstOrDefaultAsync(ct);

        if (latest is not null && !latest.IsExpired())
        {
            var elapsed = (DateTime.UtcNow - (latest.CreateDate ?? DateTime.UtcNow)).TotalSeconds;
            if (elapsed < _options.ResendCooldownSeconds)
            {
                result.ResendAfterSeconds = (int)Math.Ceiling(_options.ResendCooldownSeconds - elapsed);
                return result; // Hâlâ soğuma süresinde — yeni token üretmeyiz, dağıtım yapmayız.
            }
        }

        await IssueAndDispatchAsync(user, email, result.MaskedTarget, ct);
        return result;
    }

    /// <summary>
    /// Yeni token üretir, önceki bekleyen kayıtları geçersiz kılar (yalnızca en yenisi çalışsın) ve doğrulama
    /// e-postasını dağıtır. Ham token yalnızca dağıtımda (verifyUrl) görünür — geri döndürülmez.
    /// </summary>
    private async Task IssueAndDispatchAsync(UserEntity user, string email, string maskedTarget, CancellationToken ct)
    {
        // Önceki bekleyen tokenları geçersiz kıl.
        var pending = await _db.ProviderEmailVerifications
            .Where(r => r.UserId == user.Id && r.ConsumedAt == null)
            .ToListAsync(ct);
        foreach (var p in pending) p.MarkConsumed();

        var secret = PasswordRecoverySecurity.GenerateOpaqueToken();
        var (tokenHash, tokenSalt) = PasswordRecoverySecurity.Hash(secret);

        var entity = ProviderEmailVerificationEntity.Create(
            userId: user.Id,
            tokenHash: tokenHash,
            tokenSalt: tokenSalt,
            expiresAt: DateTime.UtcNow.AddSeconds(_options.TokenTtlSeconds));

        _db.ProviderEmailVerifications.Add(entity);
        await _db.SaveChangesAsync(ct);

        // Token = {Id}.{secret}; Id ancak kayıt sonrası bilinir.
        var rawToken = $"{entity.Id}.{secret}";
        var verifyUrl = string.Format(_options.VerifyUrlTemplate, Uri.EscapeDataString(rawToken));
        var expiresInMinutes = (int)Math.Ceiling(_options.TokenTtlSeconds / 60.0);

        try
        {
            await _notifier.SendVerificationAsync(user.Id, email, maskedTarget, verifyUrl, expiresInMinutes, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Email verification: dispatch failed for {MaskedTarget}.", maskedTarget);
        }
    }

    private static bool TryParseToken(string token, out long id, out string secret)
    {
        id = 0;
        secret = string.Empty;
        if (string.IsNullOrWhiteSpace(token)) return false;

        var separator = token.IndexOf('.');
        if (separator <= 0 || separator >= token.Length - 1) return false;

        var idPart = token[..separator];
        if (!long.TryParse(idPart, out id) || id <= 0) return false;

        secret = token[(separator + 1)..];
        return true;
    }

    /// <summary>Yeniden gönderme için tanımlayıcı (e-posta) bazlı hız sınırı. Cache hatasında açık başarısız olur.</summary>
    private async Task<bool> IsIdentifierThrottledAsync(string identifier)
    {
        if (_options.MaxRequestsPerIdentifierPerWindow <= 0) return false;

        var key = $"{ResendRateLimitKeyPrefix}{Sha256Hex(identifier.ToLowerInvariant())}";
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
            _logger.LogWarning(ex, "Email verification: per-identifier rate limit cache error; failing open.");
            return false;
        }
    }

    /// <summary>Doğrulama/tüketim için token tutamağı (Id) bazlı mütevazı hız sınırı. Cache hatasında açık başarısız olur.</summary>
    private async Task<bool> IsVerifyThrottledAsync(long id)
    {
        if (_options.MaxVerifyAttemptsPerWindow <= 0) return false;

        var key = $"{VerifyRateLimitKeyPrefix}{id}";
        var window = TimeSpan.FromSeconds(_options.VerifyWindowSeconds);
        try
        {
            var current = await _cache.GetNoHash<int>(key);
            if (current >= _options.MaxVerifyAttemptsPerWindow) return true;
            await _cache.SetNoHash(key, current + 1, window);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Email verification: per-token rate limit cache error; failing open.");
            return false;
        }
    }

    private static string Sha256Hex(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
