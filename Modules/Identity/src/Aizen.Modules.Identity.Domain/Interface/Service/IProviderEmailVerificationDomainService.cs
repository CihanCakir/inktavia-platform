using Aizen.Modules.Identity.Domain.Model.EmailVerification;

namespace Aizen.Modules.Identity.Domain.Interface.Service;

/// <summary>
/// Sağlayıcı e-posta doğrulama domaini — ASP.NET Core Identity'nin YERLEŞİK e-posta onay token'ı üzerine kurulu
/// (UserManager.GenerateEmailConfirmationTokenAsync / ConfirmEmailAsync). Token bir DataProtection payload'ıdır
/// (kullanıcı id + amaç + SecurityStamp taşır); tabloda satır DEĞİLDİR ve tek kalıcı etki EmailConfirmed'dir.
///
/// KRİTİK: Bu token KİMSEYİ KİMLİK DOĞRULAMAZ; hiçbir oturum/çerez/JWT üretmez — yalnızca e-posta adresini
/// doğrular. Kullanıcı sonrasında yine parolasıyla giriş yapar.
///
/// Token IDEMPOTENT olduğundan custom tasarımın verify/consume ayrımı KALDIRILDI: <see cref="ConfirmAsync"/>
/// güvenle tekrar çağrılabilir. Akış sırası (BFF): önce ConfirmAsync (bizim DB), sonra Keycloak emailVerified=true.
/// Keycloak patlarsa kullanıcı aynı linke tekrar tıklar ve çalışır.
/// </summary>
public interface IProviderEmailVerificationDomainService
{
    /// <summary>Kayıt sonrası doğrulama token'ı üretir ve doğrulama e-postasını dağıtır.</summary>
    Task<EmailVerificationGenerateResult> GenerateAsync(string email, CancellationToken ct);

    /// <summary>Birleşik token'ı ({userId}.{Base64Url(token)}) onaylar → EmailConfirmed=true. Idempotent.</summary>
    Task<EmailVerificationConfirmResult> ConfirmAsync(string token, CancellationToken ct);

    /// <summary>E-posta ile yeniden doğrulama linki gönderir. Numaralandırma korumalı + hız sınırlı.</summary>
    Task<EmailVerificationResendResult> ResendAsync(string email, CancellationToken ct);
}
