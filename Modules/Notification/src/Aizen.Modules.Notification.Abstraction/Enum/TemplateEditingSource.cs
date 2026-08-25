namespace Aizen.Modules.Notification.Abstraction.Enum;

/// <summary>
/// Bir (channel, locale) hücresi için editörün YÜKLEDİĞİ içeriğin kaynağı. Editör körlemesine yazmasın diye,
/// düzenleme öncesi mevcut içeriğin nereden geldiğini (taslak mı yayınlanan mı) etiketlemekte kullanılır.
/// </summary>
public enum TemplateEditingSource
{
    /// <summary>Ne Draft ne Published var — düzenlenecek mevcut içerik yok (boş başlar).</summary>
    None      = 0,
    /// <summary>Mevcut Draft satırı yüklendi (Published'a göre önceliklidir).</summary>
    Draft     = 1,
    /// <summary>Draft yok; mevcut Published satırı yüklendi (düzenleme bunun üstünden yeni Draft üretir).</summary>
    Published = 2,
}
