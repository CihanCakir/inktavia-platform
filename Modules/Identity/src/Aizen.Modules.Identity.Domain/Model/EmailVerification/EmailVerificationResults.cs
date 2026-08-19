namespace Aizen.Modules.Identity.Domain.Model.EmailVerification;

/// <summary>
/// <c>IProviderEmailVerificationDomainService</c> tarafından dönen domain-içi sonuç modelleri. Bunlar sınır-ötesi
/// HTTP sözleşmeleri DEĞİLDİR — Application katmanı bunları Abstraction DTO'larına eşler. Domain'in Abstraction'a
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

public sealed class EmailVerificationVerifyResult
{
    public bool Verified { get; set; }

    /// <summary>Yalnızca başarıda dolar — BFF, Keycloak'ta hangi kullanıcının emailVerified'ını çevireceğini bilsin.</summary>
    public long? UserId { get; set; }
    public string? KeycloakSubjectId { get; set; }
    public string? Email { get; set; }

    public string Message { get; set; } = string.Empty;
}

public sealed class EmailVerificationConsumeResult
{
    public bool Consumed { get; set; }
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
