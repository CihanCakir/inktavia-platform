using Aizen.Modules.Notification.Abstraction.Enum;

namespace Aizen.Modules.Notification.Abstraction.Dto;

/// <summary>(channel, locale) için bir sürüm satırı özeti (versiyon listesi).</summary>
public sealed class NotificationTemplateVersionDto
{
    public long                  Id        { get; init; }
    public int                   Version   { get; init; }
    public TemplateContentStatus Status    { get; init; }
    public DateTimeOffset        CreatedAt { get; init; }
    public DateTimeOffset?       UpdatedAt { get; init; }
}
