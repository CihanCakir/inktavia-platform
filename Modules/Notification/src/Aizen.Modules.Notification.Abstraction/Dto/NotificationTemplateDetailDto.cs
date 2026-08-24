using Aizen.Modules.Notification.Abstraction.Enum;

namespace Aizen.Modules.Notification.Abstraction.Dto;

/// <summary>Mantıksal template + içerik matrisi (channel→locale hücreleri). GET {code} detay yanıtı.</summary>
public sealed class NotificationTemplateDetailDto
{
    public long                Id           { get; init; }
    public string              TemplateCode { get; init; } = default!;
    public string              Name         { get; init; } = default!;
    public string?             Description  { get; init; }
    public NotificationType    Type         { get; init; }
    public NotificationChannel Channel      { get; init; }
    public bool                IsActive     { get; init; }

    /// <summary>(channel, locale) hücreleri; yayın/taslak sürümleri ve durumu taşır.</summary>
    public IReadOnlyList<NotificationTemplateContentCellDto> Contents { get; init; }
        = new List<NotificationTemplateContentCellDto>();
}
