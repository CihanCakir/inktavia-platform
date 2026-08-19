using Aizen.Core.Domain;

namespace Aizen.Modules.Identity.Domain.Entities.EmailVerification;

/// <summary>
/// Uygulama sahipliğindeki sağlayıcı e-posta doğrulama kaydı. Ham token ASLA saklanmaz — yalnızca
/// tuzlu PBKDF2 özeti (<see cref="TokenHash"/> + <see cref="TokenSalt"/>) tutulur.
///
/// Token biçimi: <c>{Id}.{secret}</c>. Tuzlu özet sorgu anahtarı olamayacağından (her satırda tuz farklı),
/// satırı bulmak için opak-olmayan bir tutamağa ihtiyaç var; bu tutamak birincil anahtar <see cref="AizenEntity.Id"/>'dir.
/// Gerçek güvenlik sınırı 256-bit rastgele <c>secret</c>'tır; Id yalnızca kaydı bulmaya yarar, tahmin edilmesi
/// bir işe yaramaz çünkü secret sabit-zamanlı doğrulanır.
///
/// Bu token KİMSEYİ KİMLİK DOĞRULAMAZ; yalnızca Keycloak'ta emailVerified bayrağını çevirmek için tüketilir.
/// Kullanıcı sonrasında yine parolasıyla giriş yapar (bkz. FAZ 1 tasarım kararı: geniş olmayan etki alanı → uzun TTL).
/// </summary>
public class ProviderEmailVerificationEntity : AizenEntityWithAudit
{
    public long UserId { get; set; }

    /// <summary>Ham token'ın secret bileşeninin tuzlu PBKDF2 özeti. Ham token burada TUTULMAZ.</summary>
    public string TokenHash { get; set; } = default!;

    public string TokenSalt { get; set; } = default!;

    public DateTime ExpiresAt { get; set; }

    public DateTime? ConsumedAt { get; set; }

    protected ProviderEmailVerificationEntity() { }

    public static ProviderEmailVerificationEntity Create(
        long userId, string tokenHash, string tokenSalt, DateTime expiresAt)
    {
        return new ProviderEmailVerificationEntity
        {
            UserId = userId,
            TokenHash = tokenHash,
            TokenSalt = tokenSalt,
            ExpiresAt = expiresAt,
            CreateDate = DateTime.UtcNow,
        };
    }

    public void MarkConsumed() => ConsumedAt = DateTime.UtcNow;

    public bool IsConsumed() => ConsumedAt is not null;

    public bool IsExpired() => ExpiresAt <= DateTime.UtcNow;

    /// <summary>Kayıt hâlâ tüketilebilir mi (tüketilmemiş ve süresi dolmamış).</summary>
    public bool CanBeConsumed() => !IsConsumed() && !IsExpired();
}
