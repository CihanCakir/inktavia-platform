namespace Aizen.Modules.Notification.Abstraction.Request;

public sealed class UpdateNotificationTemplateRequest
{
    public string Name          { get; set; } = default!;
    public string TitleTemplate { get; set; } = default!;
    public string BodyTemplate  { get; set; } = default!;
}
