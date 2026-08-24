namespace Aizen.Modules.Notification.Abstraction.Request;

/// <summary>Mantıksal template meta güncellemesi: ad + açıklama + etkinleştirme. İçerik metnine dokunmaz.</summary>
public sealed class UpdateNotificationTemplateMetaRequest
{
    public string  Name        { get; set; } = default!;
    public string? Description  { get; set; }
    public bool    IsActive    { get; set; }
}
