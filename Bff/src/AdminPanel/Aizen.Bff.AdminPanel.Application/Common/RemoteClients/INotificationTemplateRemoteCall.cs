using Aizen.Core.Infrastructure.Api;
using Aizen.Core.RemoteCall.Abstraction;
using Aizen.Modules.Notification.Abstraction.Dto;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Abstraction.Request;
using Aizen.Modules.Notification.Abstraction.Response;
using Refit;

namespace Aizen.Bff.AdminPanel.Application.Common.RemoteClients;

[DocumentationInfo("Notification admin BFF remote call",
    "Defines synchronous BFF-to-Notification calls for managing notification templates (Phase-2 channel×locale×version). " +
    "Auth headers are injected automatically by AdminPanelBffAuthDelegatingHandler.")]
public interface INotificationTemplateRemoteCall : IAizenRemoteCall
{
    // ─── Paged list + detail matrix ──────────────────────────────────────────────
    [AizenRemoteCallGet("/api/v1/notification/admin/notification-templates/paged")]
    Task<AizenApiResponse<NotificationTemplateListResult>> GetTemplatesPaged(
        [Query] NotificationChannel? channel = null,
        [Query] string? locale = null,
        [Query] TemplateContentStatus? status = null,
        [Query] bool? enabled = null,
        [Query] string? search = null,
        [Query] int page = 1,
        [Query] int pageSize = 20);

    [AizenRemoteCallGet("/api/v1/notification/admin/notification-templates/{code}/detail")]
    Task<AizenApiResponse<NotificationTemplateDetailDto>> GetTemplateDetail(string code);

    [AizenRemoteCallGet("/api/v1/notification/admin/notification-templates/{code}/contents/{channel}/{locale}")]
    Task<AizenApiResponse<NotificationTemplateContentEditResult>> GetTemplateContentForEdit(
        string code, NotificationChannel channel, string locale);

    // ─── Logical template mutations ──────────────────────────────────────────────
    [AizenRemoteCallPost("/api/v1/notification/admin/notification-templates")]
    Task<AizenApiResponse<NotificationTemplateMutationResponse>> CreateNotificationTemplate(
        [AizenRemoteCallBody] CreateNotificationTemplateRemoteRequest body);

    [AizenRemoteCallPut("/api/v1/notification/admin/notification-templates/{code}/meta")]
    Task<AizenApiResponse<NotificationTemplateMutationResponse>> UpdateTemplateMeta(
        string code,
        [AizenRemoteCallBody] UpdateNotificationTemplateMetaRequest body);

    [AizenRemoteCallPatch("/api/v1/notification/admin/notification-templates/{code}/toggle")]
    Task<AizenApiResponse<NotificationTemplateMutationResponse>> ToggleNotificationTemplate(string code);

    // ─── Content (channel×locale×version) ───────────────────────────────────────
    [AizenRemoteCallPut("/api/v1/notification/admin/notification-templates/{code}/contents/{channel}/{locale}")]
    Task<AizenApiResponse<NotificationTemplateContentDto>> SaveTemplateContentDraft(
        string code, NotificationChannel channel, string locale,
        [AizenRemoteCallBody] SaveNotificationTemplateContentRequest body);

    [AizenRemoteCallPost("/api/v1/notification/admin/notification-templates/{code}/contents/{channel}/{locale}/publish")]
    Task<AizenApiResponse<NotificationTemplateContentDto>> PublishTemplateContent(
        string code, NotificationChannel channel, string locale);

    [AizenRemoteCallGet("/api/v1/notification/admin/notification-templates/{code}/versions")]
    Task<AizenApiResponse<List<NotificationTemplateVersionDto>>> GetTemplateVersions(
        string code, [Query] NotificationChannel channel, [Query] string locale);

    // ─── Preview + variables catalog ─────────────────────────────────────────────
    [AizenRemoteCallPost("/api/v1/notification/admin/notification-templates/{code}/preview")]
    Task<AizenApiResponse<NotificationTemplatePreviewResultDto>> PreviewTemplateContent(
        string code, [AizenRemoteCallBody] NotificationTemplatePreviewRequest body);

    [AizenRemoteCallGet("/api/v1/notification/admin/notification-templates/{code}/variables")]
    Task<AizenApiResponse<NotificationTemplateVariablesDto>> GetTemplateVariables(string code);
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
