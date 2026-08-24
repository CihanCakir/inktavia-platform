using Aizen.Modules.Notification.Abstraction.Enum;

namespace Aizen.Modules.Notification.Abstraction.Request;

/// <summary>Preview isteği: hangi kanal/locale, hangi değişkenlerle render edilecek.</summary>
public sealed class NotificationTemplatePreviewRequest
{
    public NotificationChannel        Channel   { get; set; }
    public string                     Locale    { get; set; } = default!;
    public Dictionary<string, string> Variables { get; set; } = new();
}
