using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Domain.Model;

namespace Aizen.Modules.Notification.Domain.Interface.Service;

/// <summary>
/// Üretim ve (ileride) preview tarafından KULLANILAN TEK strict renderer. Bir (templateCode, channel) için Published
/// içeriği locale fallback zinciriyle seçer (istenen → 'en' → ilk Published); hiç yoksa TemplateContentMissingException
/// atar. Placeholder'lar STRICT doldurulur: variables'ta olmayan {{key}} → hata (eksik anahtarları listeler).
/// Email için HtmlTemplate çıktısı layout kabuğunun {{content}}'ine sarılır.
/// </summary>
public interface ITemplateRenderer
{
    Task<RenderedContent> RenderAsync(
        string templateCode,
        NotificationChannel channel,
        string locale,
        IReadOnlyDictionary<string, string> variables,
        CancellationToken ct = default);
}
