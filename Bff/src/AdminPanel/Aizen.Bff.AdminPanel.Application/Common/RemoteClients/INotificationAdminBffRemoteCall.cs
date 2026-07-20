using Aizen.Core.Infrastructure.Api;
using Aizen.Core.RemoteCall.Abstraction;
using Aizen.Modules.Notification.Abstraction.Dto;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Abstraction.Response;

namespace Aizen.Bff.AdminPanel.Application.Common.RemoteClients;

[DocumentationInfo("Notification admin BFF remote call",
    "Defines synchronous BFF-to-Notification calls for managing notification templates. " +
    "Auth headers are injected automatically by AdminPanelBffAuthDelegatingHandler.")]
public interface INotificationAdminBffRemoteCall : IAizenRemoteCall
{
    [AizenRemoteCallGet("/api/v1/notification/admin/notification-templates")]
    Task<AizenApiResponse<List<NotificationTemplateDto>>> GetNotificationTemplates();

    [AizenRemoteCallGet("/api/v1/notification/admin/notification-templates/{code}")]
    Task<AizenApiResponse<NotificationTemplateDto>> GetNotificationTemplateByCode(string code);

    [AizenRemoteCallPost("/api/v1/notification/admin/notification-templates")]
    Task<AizenApiResponse<NotificationTemplateMutationResponse>> CreateNotificationTemplate(
        [AizenRemoteCallBody] CreateNotificationTemplateRemoteRequest body);

    [AizenRemoteCallPut("/api/v1/notification/admin/notification-templates/{code}")]
    Task<AizenApiResponse<NotificationTemplateMutationResponse>> UpdateNotificationTemplate(
        string code,
        [AizenRemoteCallBody] UpdateNotificationTemplateRemoteRequest body);

    [AizenRemoteCallPatch("/api/v1/notification/admin/notification-templates/{code}/toggle")]
    Task<AizenApiResponse<NotificationTemplateMutationResponse>> ToggleNotificationTemplate(string code);
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
