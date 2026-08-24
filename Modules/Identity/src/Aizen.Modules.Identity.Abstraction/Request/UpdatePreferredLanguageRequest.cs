namespace Aizen.Modules.Identity.Abstraction.Request
{
    /// <summary>
    /// Kullanıcının kalıcı arayüz dili tercihini güncelleme isteği. Bölge-siz kod ("tr"/"en") beklenir; entity
    /// normalize eder ("en-US" → "en") ve makul değilse null saklar.
    /// </summary>
    public sealed class UpdatePreferredLanguageRequest
    {
        public string? PreferredLanguage { get; set; }
    }
}
