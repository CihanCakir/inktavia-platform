namespace Aizen.Modules.Notification.Application.Services;

/// <summary>
/// Bildirim modülünün yerelleştirme (locale) ayarları. EmailOptions ile AYNI şekilde
/// "Notification" konfigürasyon bölümünden bağlanır (services.Configure&lt;NotificationOptions&gt;).
/// </summary>
public sealed class NotificationOptions
{
    public const string SectionName = "Notification";

    /// <summary>Hiçbir kaynak geçerli bir kod vermezse kullanılacak son çare locale. Varsayılan "tr".</summary>
    public string DefaultLocale { get; set; } = "tr";

    /// <summary>
    /// İzin verilen locale kodları (bölge-siz: "tr"/"en"). ReferenceData'ya çözüm başına GİTMEMEK için
    /// bilinçli olarak konfigürasyondan yüklenen sabit bir allowlist. Bir aday kod ancak bu listedeyse kabul edilir.
    /// </summary>
    public List<string> AllowedLocales { get; set; } = new() { "tr", "en" };
}
