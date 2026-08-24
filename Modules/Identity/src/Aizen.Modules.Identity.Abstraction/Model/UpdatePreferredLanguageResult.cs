namespace Aizen.Modules.Identity.Abstraction.Model
{
    /// <summary>
    /// Kullanıcının kalıcı dil tercihini güncelleme sonucu. <see cref="PreferredLanguage"/>, entity tarafından
    /// normalize/doğrulama sonrası SAKLANAN değerdir (geçersiz giriş verildiyse null).
    /// </summary>
    public sealed record UpdatePreferredLanguageResult(bool Success, string? PreferredLanguage);
}
