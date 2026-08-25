using Aizen.Modules.Notification.Abstraction.Enum;

namespace Aizen.Modules.Notification.Abstraction.Dto;

/// <summary>
/// Bir (channel, locale) hücresi için DÜZENLENEBİLİR mevcut içerik: varsa mevcut Draft, yoksa mevcut Published,
/// ikisi de yoksa boş sonuç. Editör, düzenlemeden önce ekranda ne olduğunu göstermek ve neyi yüklediğini
/// (<see cref="EditingSource"/>) etiketlemek için kullanır — böylece 66 migrate template körlemesine ezilmez.
/// </summary>
public sealed class NotificationTemplateContentEditResult
{
    /// <summary>Düzenlenecek mevcut bir içerik (Draft ya da Published) bulundu mu.</summary>
    public bool                            Found         { get; init; }

    /// <summary>Yüklenen içeriğin kaynağı (0=None, 1=Draft, 2=Published).</summary>
    public TemplateEditingSource           EditingSource { get; init; }

    /// <summary>Yüklenen içerik satırı; <see cref="Found"/> false ise null.</summary>
    public NotificationTemplateContentDto? Content       { get; init; }
}
