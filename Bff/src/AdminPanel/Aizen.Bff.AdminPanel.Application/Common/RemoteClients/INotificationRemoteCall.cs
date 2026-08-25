using Aizen.Bff.AdminPanel.Application.Notifications.Dto;
using Aizen.Core.Infrastructure.Api;
using Aizen.Core.RemoteCall.Abstraction;
using Aizen.Modules.Notification.Abstraction.Dto;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Abstraction.Request;
using Aizen.Modules.Notification.Abstraction.Response;

namespace Aizen.Bff.AdminPanel.Application.Common.RemoteClients;

[DocumentationInfo("Notification user BFF remote call", "Forwards admin notification inbox requests to the Notification module. Auth headers (Authorization service token + optional X-Aizen-Bff-Assertion) are injected by AdminPanelBffAuthDelegatingHandler.")]
public interface INotificationRemoteCall : IAizenRemoteCall
{
    [AizenRemoteCallGet("/api/v1/notification/notifications")]
    Task<AizenApiResponse<NotificationListBffDto>> GetMyNotificationsAsync(
        [Refit.Query] int skip     = 0,
        [Refit.Query] int take     = 20,
        CancellationToken ct = default);

    // ─── Faz 28.5 admin gönderim geçmişi (tüm kanallar) ─────────────────────────
    [AizenRemoteCallGet("/api/v1/notification/admin/notifications/history")]
    Task<AizenApiResponse<NotificationHistoryListResult>> GetNotificationHistoryAsync(
        [Refit.Query] DateTimeOffset? from             = null,
        [Refit.Query] DateTimeOffset? to               = null,
        [Refit.Query] NotificationChannel? channel     = null,
        [Refit.Query] NotificationStatus? status       = null,
        [Refit.Query] string? templateCode             = null,
        [Refit.Query] long? recipientUserId            = null,
        [Refit.Query] long? campaignId                 = null,
        [Refit.Query] int page                         = 1,
        [Refit.Query] int pageSize                     = 20,
        CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/notification/admin/notifications/history/{id}")]
    Task<AizenApiResponse<NotificationHistoryDetailDto>> GetNotificationHistoryByIdAsync(
        long id, CancellationToken ct = default);

    // Faz 28.6 admin kampanyalar (doğrudan/toplu bildirim)
    [AizenRemoteCallPost("/api/v1/notification/admin/notifications/campaigns")]
    Task<AizenApiResponse<NotificationCampaignMutationResponse>> CreateNotificationCampaignAsync(
        [AizenRemoteCallBody] CreateNotificationCampaignRequest body);

    [AizenRemoteCallGet("/api/v1/notification/admin/notifications/campaigns/{id}")]
    Task<AizenApiResponse<NotificationCampaignDto>> GetNotificationCampaignByIdAsync(long id);

    [AizenRemoteCallGet("/api/v1/notification/admin/notifications/campaigns")]
    Task<AizenApiResponse<NotificationCampaignListResult>> GetNotificationCampaignsPagedAsync(
        [Refit.Query] int page = 1, [Refit.Query] int pageSize = 20);

    [AizenRemoteCallPatch("/api/v1/notification/notifications/{id}/read")]
    Task<AizenApiResponse<object>> MarkReadAsync(long id, CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/notification/notifications/mark-all-read")]
    Task<AizenApiResponse<object>> MarkAllReadAsync(CancellationToken ct = default);

    // ─── Web push (N-A) ─────────────────────────────────────────────────────────
    // The module resolves the subscribing/recipient identity from the forwarded user assertion
    // (ProviderProfileId → UserId), same as the inbox calls above, so the token is stored against
    // the admin's UserId. Envelope-correct (AizenApiResponse<T>); the controller unwraps .Body.

    [AizenRemoteCallGet("/api/v1/notification/notifications/vapid-public-key")]
    Task<AizenApiResponse<VapidPublicKeyResponse>> GetVapidPublicKeyAsync(CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/notification/notifications/push-subscriptions")]
    Task<AizenApiResponse<PushSubscriptionResponse>> RegisterWebPushSubscriptionAsync(
        [AizenRemoteCallBody] PushSubscriptionRequest body,
        CancellationToken ct = default);

    [AizenRemoteCallDelete("/api/v1/notification/notifications/push-subscriptions")]
    Task<AizenApiResponse<PushSubscriptionResponse>> DeactivateWebPushSubscriptionAsync(
        [AizenRemoteCallBody] PushUnsubscribeRequest body,
        CancellationToken ct = default);

    // ─── N-B notification preferences ────────────────────────────────────────────
    [AizenRemoteCallGet("/api/v1/notification/notifications/preferences")]
    Task<AizenApiResponse<NotificationPreferencesResponse>> GetPreferencesAsync(CancellationToken ct = default);

    [AizenRemoteCallPut("/api/v1/notification/notifications/preferences")]
    Task<AizenApiResponse<NotificationPreferencesResponse>> UpdatePreferenceAsync(
        [AizenRemoteCallBody] UpdateNotificationPreferenceRequest body,
        CancellationToken ct = default);
}
