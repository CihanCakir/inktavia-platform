using Aizen.Core.Infrastructure.Api;
using Aizen.Core.RemoteCall.Abstraction;
using Aizen.Modules.Notification.Abstraction.Dto;
using Aizen.Modules.Notification.Abstraction.Enum;

namespace Aizen.Bff.AdminPanel.Application.Common.RemoteClients;

[DocumentationInfo("Notification admin BFF remote call", "Defines synchronous BFF-to-Notification calls for managing notification templates.")]
public interface INotificationAdminBffRemoteCall : IAizenRemoteCall
{
    [AizenRemoteCallGet("/api/v1/notification/admin/notification-templates")]
    Task<AizenApiResponse<List<NotificationTemplateDto>>> GetNotificationTemplates(
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken);

    [AizenRemoteCallGet("/api/v1/notification/admin/notification-templates/{code}")]
    Task<AizenApiResponse<NotificationTemplateDto>> GetNotificationTemplateByCode(
        string code,
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken);

    [AizenRemoteCallPost("/api/v1/notification/admin/notification-templates")]
    Task<AizenApiResponse<object>> CreateNotificationTemplate(
        [AizenRemoteCallBody] CreateNotificationTemplateRemoteRequest body,
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken);

    [AizenRemoteCallPut("/api/v1/notification/admin/notification-templates/{code}")]
    Task<AizenApiResponse<object>> UpdateNotificationTemplate(
        string code,
        [AizenRemoteCallBody] UpdateNotificationTemplateRemoteRequest body,
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken);

    [AizenRemoteCallPatch("/api/v1/notification/admin/notification-templates/{code}/toggle")]
    Task<AizenApiResponse<object>> ToggleNotificationTemplate(
        string code,
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken);
}

public sealed class CreateNotificationTemplateRemoteRequest
{
    public string              TemplateCode  { get; set; } = default!;
    public string              Name          { get; set; } = default!;
    public NotificationType    Type          { get; set; }
    public NotificationChannel Channel       { get; set; }
    public string              TitleTemplate { get; set; } = default!;
    public string              BodyTemplate  { get; set; } = default!;
}

public sealed class UpdateNotificationTemplateRemoteRequest
{
    public string Name          { get; set; } = default!;
    public string TitleTemplate { get; set; } = default!;
    public string BodyTemplate  { get; set; } = default!;
}
