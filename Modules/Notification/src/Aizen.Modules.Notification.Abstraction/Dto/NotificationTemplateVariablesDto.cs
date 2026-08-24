using Aizen.Modules.Notification.Abstraction.Enum;

namespace Aizen.Modules.Notification.Abstraction.Dto;

/// <summary>Bir template'in tipine göre izin verilen {{placeholder}} adları (değişken kataloğu).</summary>
public sealed class NotificationTemplateVariablesDto
{
    public string               TemplateCode { get; init; } = default!;
    public NotificationType     Type         { get; init; }
    public IReadOnlyList<string> Placeholders { get; init; } = new List<string>();
}
