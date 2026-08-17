using Aizen.Core.Infrastructure.Api;
using Aizen.Core.RemoteCall.Abstraction;
using Aizen.Modules.Notification.Abstraction.Request;
using Aizen.Modules.Notification.Abstraction.Response;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;

/// <summary>
/// BFF → Notification module owner endpoints (BE_MO9c, <c>api/v1/notification/notifications</c>). Every module handler
/// resolves the recipient from the trusted context (<c>KeycloakTokenInfo.ProviderProfileId</c> — the BffAssertion the
/// mobile BFF sends via <c>X-Aizen-Provider-Profile-Id</c> — else <c>UserInfo.UserId</c>), so the inbox / device-token
/// / preferences scope to the caller's participant automatically — no owner-gate to add, no recipient in the body.
/// The WebPush endpoints (push-subscriptions / vapid-public-key) are deliberately NOT exposed to the mobile app.
/// </summary>
public interface INotificationRemoteCall : IAizenRemoteCall
{
    // The caller's notification inbox (paged; Total + UnreadCount for the badge).
    [AizenRemoteCallGet("/api/v1/notification/notifications")]
    Task<AizenApiResponse<NotificationListResponse>> GetUserNotifications(
        [Refit.Query] int skip, [Refit.Query] int take);

    // Mark one of the caller's notifications read.
    [AizenRemoteCallPatch("/api/v1/notification/notifications/{id}/read")]
    Task<AizenApiResponse<MarkNotificationReadResponse>> MarkAsRead(long id);

    // Mark all of the caller's notifications read.
    [AizenRemoteCallPost("/api/v1/notification/notifications/mark-all-read")]
    Task<AizenApiResponse<MarkAllNotificationsReadResponse>> MarkAllAsRead();

    // Register an FCM device token for the caller (feeds MO9a push). Platform is forced to Fcm BFF-side.
    [AizenRemoteCallPost("/api/v1/notification/notifications/device-token")]
    Task<AizenApiResponse<DeviceTokenResponse>> RegisterDeviceToken(
        [AizenRemoteCallBody] RegisterDeviceTokenRequest request);

    // The caller's per-category channel preference matrix (InApp/Push/Email + locked flags).
    [AizenRemoteCallGet("/api/v1/notification/notifications/preferences")]
    Task<AizenApiResponse<NotificationPreferencesResponse>> GetPreferences();

    // Toggle one category×channel preference (only Push/Email are user-changeable; the module rejects locked cells).
    [AizenRemoteCallPut("/api/v1/notification/notifications/preferences")]
    Task<AizenApiResponse<NotificationPreferencesResponse>> UpdatePreference(
        [AizenRemoteCallBody] UpdateNotificationPreferenceRequest request);
}
