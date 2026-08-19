namespace Aizen.Modules.Identity.Repository.Identity.Service.EmailVerification;

/// <summary>
/// Sağlayıcı e-posta doğrulama akışının ayarları. Eşikler OtpLoginOptions/PasswordRecoveryOptions ile hizalıdır;
/// TEK bilinçli sapma <see cref="TokenTtlSeconds"/>'dir (bkz. açıklaması).
/// </summary>
public sealed class ProviderEmailVerificationOptions
{
    public const string SectionName = "ProviderEmailVerification";

    /// <summary>
    /// Token yaşam süresi. OTP değeri 300s'tir çünkü insanın ekrandan okuyup hemen girdiği bir koddur.
    /// Bu token ise bir e-posta LİNKİDİR: teslim gecikmesi + kullanıcının gelen kutusunu açması için yaşamalı.
    /// Ayrıca token KİMSEYİ KİMLİK DOĞRULAMAZ (yalnızca emailVerified bayrağını çevirir) ve 256-bit rastgele
    /// secret brute-force edilemez; bu yüzden 24 saat güvenli ve kullanılabilir. (Keycloak'ın bu realm'deki
    /// action token'ı da exp−iat = 43200s / 12s kullanıyordu.)
    /// </summary>
    public int TokenTtlSeconds { get; set; } = 86400;

    /// <summary>Yeniden gönderme bekleme süresi. OtpLogin/PasswordRecovery ile hizalı.</summary>
    public int ResendCooldownSeconds { get; set; } = 60;

    /// <summary>Yeniden gönderme: aynı tanımlayıcı (e-posta) için pencere başına azami istek. 0 = kapalı.</summary>
    public int MaxRequestsPerIdentifierPerWindow { get; set; } = 5;

    /// <summary>Yeniden gönderme hız sınırı penceresi (saniye). OtpLogin/PasswordRecovery ile hizalı.</summary>
    public int IdentifierWindowSeconds { get; set; } = 3600;

    /// <summary>
    /// Doğrulama/tüketim ucunun mütevazı hız sınırı: tek bir token tutamağının (Id) kısa pencerede
    /// deneme sayısı. 256-bit secret zaten brute-force edilemez; bu, sınırsız ucun "bedava yükselteç"
    /// olmasına karşı derinlemesine savunmadır. 0 = kapalı.
    /// </summary>
    public int MaxVerifyAttemptsPerWindow { get; set; } = 10;

    /// <summary>Doğrulama/tüketim hız sınırı penceresi (saniye).</summary>
    public int VerifyWindowSeconds { get; set; } = 60;

    /// <summary>
    /// Doğrulama linkinin şablonu. <c>{0}</c> ham token ile değiştirilir. Örn:
    /// <c>https://dev-provider.inktavia.com/onboarding/verify-email?token={0}</c>.
    /// Açık yönlendirme riskini önlemek için taban Identity ayarında tutulur, çağırandan alınmaz.
    /// </summary>
    public string VerifyUrlTemplate { get; set; } = "https://dev-provider.inktavia.com/onboarding/verify-email?token={0}";

    /// <summary>
    /// <c>Notification</c> (varsayılan) gerçek e-posta teslimi için mesaj kuyruğuna yayınlar.
    /// <c>Logging</c> yalnızca teslim niyetini loglar (yerel geliştirme).
    /// </summary>
    public string DeliveryMode { get; set; } = "Notification";

    /// <summary>
    /// YALNIZCA DEV. true iken doğrulama linki Debug seviyesinde loglanır ki geliştirici, Notification
    /// hattı bağlı olmadan akışı yerelde tamamlayabilsin. Paylaşımlı/test/prod'da MUTLAKA false kalmalı.
    /// </summary>
    public bool DevExposeVerifyUrl { get; set; } = false;
}
