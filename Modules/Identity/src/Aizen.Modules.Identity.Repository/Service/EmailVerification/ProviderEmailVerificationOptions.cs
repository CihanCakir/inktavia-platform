namespace Aizen.Modules.Identity.Repository.Identity.Service.EmailVerification;

/// <summary>
/// Sağlayıcı e-posta doğrulama akışının ayarları. Token ömrü BURADA DEĞİLDİR — o, ASP.NET Core Identity'nin
/// yerleşik "Default" token sağlayıcısına ait <c>DataProtectionTokenProviderOptions.TokenLifespan</c> ile
/// (DI'da 24 saat) belirlenir. Buradaki eşikler yeniden gönderme hız sınırı içindir ve OtpLoginOptions ile hizalıdır.
/// </summary>
public sealed class ProviderEmailVerificationOptions
{
    public const string SectionName = "ProviderEmailVerification";

    /// <summary>Yeniden gönderme bekleme süresi. OtpLogin/PasswordRecovery ile hizalı.</summary>
    public int ResendCooldownSeconds { get; set; } = 60;

    /// <summary>Yeniden gönderme: aynı tanımlayıcı (e-posta) için pencere başına azami istek. 0 = kapalı.</summary>
    public int MaxRequestsPerIdentifierPerWindow { get; set; } = 5;

    /// <summary>Yeniden gönderme hız sınırı penceresi (saniye). OtpLogin/PasswordRecovery ile hizalı.</summary>
    public int IdentifierWindowSeconds { get; set; } = 3600;

    /// <summary>
    /// Doğrulama linkinin şablonu. <c>{0}</c> URL-güvenli birleşik token ile değiştirilir
    /// (<c>{userId}.{Base64Url(token)}</c>). Örn: <c>https://dev-provider.inktavia.com/onboarding/verify-email?token={0}</c>.
    /// </summary>
    public string VerifyUrlTemplate { get; set; } = "https://dev-provider.inktavia.com/onboarding/verify-email?token={0}";

    /// <summary>
    /// <c>Notification</c> (varsayılan) gerçek e-posta teslimi için mesaj kuyruğuna yayınlar.
    /// <c>Logging</c> yalnızca teslim niyetini loglar (yerel geliştirme).
    /// </summary>
    public string DeliveryMode { get; set; } = "Notification";

    /// <summary>
    /// YALNIZCA DEV. true iken doğrulama linki Debug seviyesinde loglanır. Paylaşımlı/test/prod'da MUTLAKA false.
    /// </summary>
    public bool DevExposeVerifyUrl { get; set; } = false;
}
