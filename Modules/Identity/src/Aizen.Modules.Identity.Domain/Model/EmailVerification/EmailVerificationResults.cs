namespace Aizen.Modules.Identity.Domain.Model.EmailVerification;

/// <summary>
/// <c>IProviderEmailVerificationDomainService</c> tarafından dönen domain-içi sonuç modelleri. Sınır-ötesi HTTP
/// sözleşmeleri DEĞİLDİR — Application katmanı bunları Abstraction DTO'larına eşler. Domain'in Abstraction'a
/// bağımlı olmaması için burada tutulur.
/// </summary>
public sealed class EmailVerificationGenerateResult
{
    /// <summary>Numaralandırma korumalı: kullanıcı bulunsa da bulunmasa da true döner.</summary>
    public bool Accepted { get; set; } = true;
    public string MaskedTarget { get; set; } = string.Empty;
    public int ExpiresInSeconds { get; set; }
    public int ResendAfterSeconds { get; set; }
}

/// <summary>
/// Onay sonucunun durumu. FE bunun üzerine dallanır: SÜRESİ DOLMUŞ link için "yeniden gönder" düğmesi gösterilir,
/// GEÇERSİZ link için gösterilmez. Yerleşik token tek bir "Failed" döndürdüğünden bu ayrım servis katmanında
/// uzun-ömürlü ikinci bir sağlayıcıyla üretilir (bkz. ProviderEmailVerificationDomainService.ConfirmAsync).
/// </summary>
public enum EmailVerificationConfirmStatus
{
    Confirmed = 0,
    Expired = 1,
    Invalid = 2,
}

/// <summary>
/// Onay sonucu. Yerleşik token IDEMPOTENT olduğundan iki kez onaylamak zararsızdır — ikinci çağrı da
/// <see cref="Status"/>=Confirmed döner. Ayrı bir "tüketildi" durumu YOKTUR (custom tasarımın verify/consume
/// ayrımı kaldırıldı).
/// </summary>
public sealed class EmailVerificationConfirmResult
{
    public EmailVerificationConfirmStatus Status { get; set; } = EmailVerificationConfirmStatus.Invalid;

    /// <summary>Kısayol: Status == Confirmed.</summary>
    public bool Confirmed => Status == EmailVerificationConfirmStatus.Confirmed;

    /// <summary>Yalnızca başarıda dolar — BFF, Keycloak'ta hangi kullanıcının emailVerified'ını çevireceğini bilsin.</summary>
    public long? UserId { get; set; }
    public string? KeycloakSubjectId { get; set; }
    public string? Email { get; set; }

    public string Message { get; set; } = string.Empty;
}

public sealed class EmailVerificationResendResult
{
    /// <summary>Numaralandırma korumalı: her zaman genel bir yanıt döner.</summary>
    public bool Accepted { get; set; } = true;
    public string MaskedTarget { get; set; } = string.Empty;
    public int ResendAfterSeconds { get; set; }
    public int ExpiresInSeconds { get; set; }
}
