using Aizen.Modules.Notification.Abstraction.Enum;

namespace Aizen.Modules.Notification.Abstraction.Dto;

/// <summary>
/// İçerik matrisinde bir (channel, locale) hücresi: yayındaki sürüm, (varsa) taslak sürüm ve türetilmiş durum.
/// Status = Published varsa Published; yoksa Draft varsa Draft; ikisi de yoksa Archived (yalnız arşiv kalmışsa).
/// </summary>
public sealed class NotificationTemplateContentCellDto
{
    public NotificationChannel   Channel          { get; init; }
    public string                Locale           { get; init; } = default!;
    public int?                  PublishedVersion { get; init; }
    public int?                  DraftVersion     { get; init; }
    public TemplateContentStatus Status           { get; init; }
}
