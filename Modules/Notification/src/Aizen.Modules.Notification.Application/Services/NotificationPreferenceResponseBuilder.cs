using Aizen.Modules.Notification.Abstraction;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Abstraction.Response;
using Aizen.Modules.Notification.Domain.Entities;

namespace Aizen.Modules.Notification.Application.Services;

/// <summary>
/// Builds the N-B preferences matrix from the user's stored rows, filling absent cells with the policy
/// defaults. Only user-toggleable categories are surfaced (the security Account category is excluded).
/// </summary>
public static class NotificationPreferenceResponseBuilder
{
    private static readonly NotificationChannel[] Channels =
        { NotificationChannel.InApp, NotificationChannel.Push, NotificationChannel.Email };

    public static NotificationPreferencesResponse Build(IReadOnlyCollection<NotificationPreferenceEntity> stored)
    {
        var lookup = stored.ToDictionary(x => (x.Category, x.Channel), x => x.Enabled);

        bool? Stored(NotificationCategory c, NotificationChannel ch)
            => lookup.TryGetValue((c, ch), out var v) ? v : null;

        ChannelPreferenceDto Cell(NotificationCategory c, NotificationChannel ch) => new()
        {
            Enabled = NotificationPreferencePolicy.Resolve(c, ch, Stored(c, ch)),
            Locked  = NotificationPreferencePolicy.IsLocked(c, ch),
        };

        return new NotificationPreferencesResponse
        {
            Categories = NotificationCategoryMap.ToggleableCategories
                .Select(c => new NotificationCategoryPreferenceDto
                {
                    Category = c.ToString(),
                    InApp    = Cell(c, NotificationChannel.InApp),
                    Push     = Cell(c, NotificationChannel.Push),
                    Email    = Cell(c, NotificationChannel.Email),
                })
                .ToList(),
        };
    }
}
