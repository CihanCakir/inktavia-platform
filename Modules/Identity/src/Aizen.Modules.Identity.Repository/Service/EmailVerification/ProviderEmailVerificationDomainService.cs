using System.Security.Cryptography;
using System.Text;
using Aizen.Core.Cache.Abstraction;
using Aizen.Modules.Identity.Domain.Entities;
using Aizen.Modules.Identity.Domain.Interface.Service;
using Aizen.Modules.Identity.Domain.Model.EmailVerification;
using Aizen.Modules.Identity.Repository.Identity.Service.PasswordRecovery;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aizen.Modules.Identity.Repository.Identity.Service.EmailVerification;

/// <summary>
/// Sağlayıcı e-posta doğrulama servisi — ASP.NET Core Identity'nin YERLEŞİK e-posta onay token'ı üzerine kurulu.
/// Custom token tablosu/domaini KALDIRILDI; token bir DataProtection payload'ıdır (kullanıcı id + amaç +
/// SecurityStamp), tabloda satır değildir ve tek kalıcı etki EmailConfirmed'dir.
///
/// SERT KISIT: hiçbir oturum/çerez/JWT üretilmez. Token IDEMPOTENT olduğundan verify/consume ayrımı yoktur:
/// <see cref="ConfirmAsync"/> güvenle tekrarlanabilir. Token ömrü DataProtectionTokenProviderOptions.TokenLifespan
/// (DI'da 24 saat) ile belirlenir. İptal gerekirse kullanıcının SecurityStamp'i değiştirilir (diğer token'larını
/// da geçersiz kılar); ayrı iptal yolu kurulmaz.
///
/// Kullanıcı araması UserManager ile yapılır (elle NormalizedEmail sorgusu DEĞİL): yazma da UserManager
/// normalizer'ından geçtiğinden, elle sorgu farklı normalize ederse bazı kullanıcılar sessizce bulunamaz
/// (karışık büyük/küçük harf, Türkçe karakter, noktalı adresler).
/// </summary>
public sealed class ProviderEmailVerificationDomainService : IProviderEmailVerificationDomainService
{
    private readonly UserManager<UserEntity> _userManager;
    private readonly IProviderEmailVerificationNotifier _notifier;
    private readonly IAizenDistributedCache _cache;
    private readonly ProviderEmailVerificationOptions _options;
    private readonly int _tokenTtlSeconds;
    private readonly ILogger<ProviderEmailVerificationDomainService> _logger;

    private const string ResendRateLimitKeyPrefix = "provider:emailverify:resend:rl:";
    private const string ResendCooldownKeyPrefix = "provider:emailverify:resend:cd:";

    public ProviderEmailVerificationDomainService(
        UserManager<UserEntity> userManager,
        IProviderEmailVerificationNotifier notifier,
        IAizenDistributedCache cache,
        IOptions<ProviderEmailVerificationOptions> options,
        IOptions<DataProtectionTokenProviderOptions> tokenOptions,
        ILogger<ProviderEmailVerificationDomainService> logger)
    {
        _userManager = userManager;
        _notifier = notifier;
        _cache = cache;
        _options = options.Value;
        // Tek doğruluk kaynağı: gösterilen "kaç saniye geçerli" değeri, token'ı gerçekten sınırlayan
        // TokenLifespan'den türetilir — iki yerde 24 saati ayrı yazıp sürüklenme riskine girmeyiz.
        _tokenTtlSeconds = (int)tokenOptions.Value.TokenLifespan.TotalSeconds;
        _logger = logger;
    }

    public async Task<EmailVerificationGenerateResult> GenerateAsync(string email, CancellationToken ct)
    {
        email = email.Trim();

        var result = new EmailVerificationGenerateResult
        {
            Accepted = true,
            MaskedTarget = PasswordRecoverySecurity.MaskEmail(email.ToLowerInvariant()),
            ExpiresInSeconds = _tokenTtlSeconds,
            ResendAfterSeconds = _options.ResendCooldownSeconds,
        };

        var user = await FindUserByEmailAsync(email);
        if (user is null) return result; // numaralandırma koruması

        await IssueAndDispatchAsync(user, email, result.MaskedTarget, ct);
        return result;
    }

    public async Task<EmailVerificationConfirmResult> ConfirmAsync(string token, CancellationToken ct)
    {
        var invalid = new EmailVerificationConfirmResult
        {
            Confirmed = false,
            Message = "Doğrulama bağlantısı geçersiz veya süresi dolmuş. Lütfen yeni bir bağlantı isteyin.",
        };

        // Birleşik token: {userId}.{Base64Url(token)}. Bozuk/eksik token istisna FIRLATMADAN reddedilir.
        if (!TryParseCompositeToken(token, out var userId, out var rawToken))
            return invalid;

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null) return invalid;

        var confirm = await _userManager.ConfirmEmailAsync(user, rawToken);
        if (confirm.Succeeded)
            return Success(user);

        // Idempotency: token bu çağrıda geçersiz olsa bile e-posta ZATEN onaylıysa istenen son durum sağlanmış
        // demektir — aynı sonucu döneriz (kullanıcı linke ikinci kez / süre dolduktan sonra tıklamış olabilir).
        if (user.EmailConfirmed)
            return Success(user);

        return invalid;
    }

    public async Task<EmailVerificationResendResult> ResendAsync(string email, CancellationToken ct)
    {
        email = email.Trim();

        // Numaralandırma koruması: kullanıcı bulunsa da bulunmasa da aynı genel yanıt döner.
        var result = new EmailVerificationResendResult
        {
            Accepted = true,
            MaskedTarget = PasswordRecoverySecurity.MaskEmail(email.ToLowerInvariant()),
            ResendAfterSeconds = _options.ResendCooldownSeconds,
            ExpiresInSeconds = _tokenTtlSeconds,
        };

        if (await IsIdentifierThrottledAsync(email))
        {
            _logger.LogWarning("Email verification: per-identifier resend rate limit exceeded.");
            return result;
        }

        // Soğuma süresi: yakın zamanda gönderildiyse yeni dağıtım yapma (yine de genel yanıt dön).
        if (await IsInCooldownAsync(email))
            return result;

        var user = await FindUserByEmailAsync(email);
        if (user is null || user.EmailConfirmed) return result;

        await IssueAndDispatchAsync(user, email, result.MaskedTarget, ct);
        return result;
    }

    private EmailVerificationConfirmResult Success(UserEntity user) => new()
    {
        Confirmed = true,
        UserId = user.Id,
        KeycloakSubjectId = user.KeycloakSubjectId,
        Email = user.Email,
        Message = "E-posta doğrulandı.",
    };

    /// <summary>
    /// Yerleşik onay token'ı üretir, URL-güvenli birleşik token oluşturur, doğrulama linkini kurar ve dağıtır.
    /// Token yalnızca dağıtımda (verifyUrl) görünür; hiçbir yerde saklanmaz.
    /// </summary>
    private async Task IssueAndDispatchAsync(UserEntity user, string email, string maskedTarget, CancellationToken ct)
    {
        var rawToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);

        // Yerleşik token base64'tür ('+','/','=' içerir) — URL-güvenli DEĞİLDİR. Base64Url'e çevir; dönüşte çöz.
        // userId açık taşınır ki ConfirmEmailAsync doğrulama için kullanıcıyı bulabilsin (token tek başına
        // kullanıcıyı bize tanıtmaz — DataProtection payload'ını kullanıcının stamp'iyle çözer).
        var composite = $"{user.Id}.{Base64UrlEncode(rawToken)}";
        var verifyUrl = string.Format(_options.VerifyUrlTemplate, composite);
        var expiresInMinutes = (int)Math.Ceiling(_tokenTtlSeconds / 60.0);

        // Soğuma işaretini gönderimden hemen önce koy (dağıtım patlasa da yakın tekrarları sınırlasın).
        await MarkCooldownAsync(email);

        try
        {
            await _notifier.SendVerificationAsync(user.Id, email, maskedTarget, verifyUrl, expiresInMinutes, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Email verification: dispatch failed for {MaskedTarget}.", maskedTarget);
        }
    }

    private async Task<UserEntity?> FindUserByEmailAsync(string email)
    {
        try
        {
            return await _userManager.FindByEmailAsync(email);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Email verification: user lookup failed.");
            return null;
        }
    }

    private static bool TryParseCompositeToken(string token, out long userId, out string rawToken)
    {
        userId = 0;
        rawToken = string.Empty;
        if (string.IsNullOrWhiteSpace(token)) return false;

        var separator = token.IndexOf('.');
        if (separator <= 0 || separator >= token.Length - 1) return false;

        if (!long.TryParse(token[..separator], out userId) || userId <= 0) return false;

        try
        {
            rawToken = Base64UrlDecode(token[(separator + 1)..]);
        }
        catch (FormatException)
        {
            return false; // bozuk base64 → istisna fırlatmadan reddet
        }

        return !string.IsNullOrEmpty(rawToken);
    }

    // ── Yeniden gönderme hız sınırı / soğuma (durumsuz — cache tabanlı, tablo yok) ───────────────
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

    private async Task<bool> IsInCooldownAsync(string identifier)
    {
        if (_options.ResendCooldownSeconds <= 0) return false;
        var key = $"{ResendCooldownKeyPrefix}{Sha256Hex(identifier.ToLowerInvariant())}";
        try
        {
            return await _cache.GetNoHash<int>(key) > 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Email verification: cooldown cache error; failing open.");
            return false;
        }
    }

    private async Task MarkCooldownAsync(string identifier)
    {
        if (_options.ResendCooldownSeconds <= 0) return;
        var key = $"{ResendCooldownKeyPrefix}{Sha256Hex(identifier.ToLowerInvariant())}";
        try
        {
            await _cache.SetNoHash(key, 1, TimeSpan.FromSeconds(_options.ResendCooldownSeconds));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Email verification: cooldown set error; ignoring.");
        }
    }

    private static string Sha256Hex(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    // ── Base64Url (bağımsız; PasswordRecoverySecurity.GenerateOpaqueToken ile aynı char eşlemesi) ──
    private static string Base64UrlEncode(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static string Base64UrlDecode(string value)
    {
        var s = value.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4)
        {
            case 2: s += "=="; break;
            case 3: s += "="; break;
        }
        return Encoding.UTF8.GetString(Convert.FromBase64String(s));
    }
}
