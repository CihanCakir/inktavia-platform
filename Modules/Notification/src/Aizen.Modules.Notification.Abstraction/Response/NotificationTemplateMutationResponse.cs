namespace Aizen.Modules.Notification.Abstraction.Response;

public sealed class NotificationTemplateMutationResponse
{
    public string TemplateCode { get; init; } = default!;
    public bool   Success      { get; init; }
}
