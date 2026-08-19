using Aizen.Modules.Identity.Domain.Model.EmailVerification;

namespace Aizen.Modules.Identity.Domain.Interface.Service;

/// <summary>
/// Uygulama sahipliğindeki sağlayıcı e-posta doğrulama domainini yönetir: üretim / doğrulama / tüketim / yeniden gönderme.
///
/// KRİTİK: Bu token KİMSEYİ KİMLİK DOĞRULAMAZ. Hiçbir oturum, çerez veya JWT üretmez; yalnızca Keycloak'taki
/// emailVerified bayrağının çevrilmesini yetkilendirir. Doğrulama ve tüketim BİLEREK ayrıdır: BFF önce
/// <see cref="VerifyAsync"/> ile geçerliliği kanıtlar, sonra Keycloak'ta bayrağı çevirir, EN SON
/// <see cref="ConsumeAsync"/> ile token'ı tüketir. Ters sırada Keycloak patlarsa token yanar ve hesap kilitlenir.
/// </summary>
public interface IProviderEmailVerificationDomainService
{
    /// <summary>Kayıt sonrası (güvenilir iç çağrı) yeni doğrulama token'ı üretir ve doğrulama e-postasını dağıtır.</summary>
    Task<EmailVerificationGenerateResult> GenerateAsync(string keycloakSubjectId, string email, CancellationToken ct);

    /// <summary>Ham token'ı doğrular (TÜKETMEZ). Başarıda kullanıcı/subject bilgisini döner.</summary>
    Task<EmailVerificationVerifyResult> VerifyAsync(string token, CancellationToken ct);

    /// <summary>Ham token'ı tüketilmiş işaretler. Keycloak bayrağı çevrildikten SONRA çağrılır.</summary>
    Task<EmailVerificationConsumeResult> ConsumeAsync(string token, CancellationToken ct);

    /// <summary>E-posta ile yeniden doğrulama linki gönderir. Numaralandırma korumalı + hız sınırlı.</summary>
    Task<EmailVerificationResendResult> ResendAsync(string email, CancellationToken ct);
}
