using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Notification;
using Aizen.Modules.Notification.Abstraction.Dto;
using Aizen.Modules.Notification.Abstraction.Response;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Notification;

/// <summary>
/// Projects the Notification module DTOs → the cost-free mobile owner contracts (BE_MO9c). Drops the internal
/// MetadataJson (transaction/context ids) + Channel/Status; the NotificationType crosses as its string name (stable
/// for the client — the AdminPanel numeric-enum gotcha). The preference matrix is already cost-free (categories +
/// toggles) and passes straight through.
/// </summary>
public static class MobileNotificationMapper
{
    public static MobileNotificationDto MapNotification(NotificationDto n) => new()
    {
        Id            = n.Id,
        Type          = n.Type.ToString(),
        Title         = n.Title,
        Body          = n.Body,
        ReferenceType = n.ReferenceType,
        ReferenceId   = n.ReferenceId,
        IsRead        = n.IsRead,
        CreatedAt     = n.CreatedAt,
        ReadAt        = n.ReadAt,
        // MetadataJson / Channel / Status intentionally dropped (cost-free: the human message + deep-link ref only).
    };

    public static MobileNotificationListDto MapList(NotificationListResponse? r) => new()
    {
        Items       = r?.Items?.Select(MapNotification).ToList() ?? new List<MobileNotificationDto>(),
        Total       = r?.Total ?? 0,
        UnreadCount = r?.UnreadCount ?? 0,
    };

    public static MobileNotificationPreferencesDto MapPreferences(NotificationPreferencesResponse? r) => new()
    {
        Categories = r?.Categories?.Select(c => new MobileNotificationCategoryPreferenceDto
        {
            Category = c.Category,
            InApp    = Cell(c.InApp),
            Push     = Cell(c.Push),
            Email    = Cell(c.Email),
        }).ToList() ?? new List<MobileNotificationCategoryPreferenceDto>(),
    };

    private static MobileNotificationPreferenceCellDto Cell(ChannelPreferenceDto c) => new()
    {
        Enabled = c.Enabled,
        Locked  = c.Locked,
    };

    public static MobileMarkReadResultDto MapMarkRead(MarkNotificationReadResponse r) => new()
    {
        NotificationId = r.NotificationId,
        Updated        = r.Updated,
    };

    public static MobileMarkAllReadResultDto MapMarkAll(MarkAllNotificationsReadResponse r) => new()
    {
        UpdatedCount = r.UpdatedCount,
    };

    public static MobileDeviceTokenResultDto MapDeviceToken(DeviceTokenResponse r) => new()
    {
        Registered = r.Registered,
    };
}
