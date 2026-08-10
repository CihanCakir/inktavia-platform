using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Notification;
using Aizen.Bff.Marine.Participant.Mobile.Application.Notification;
using Aizen.Modules.Notification.Abstraction.Dto;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Abstraction.Response;
using FluentAssertions;

namespace Aizen.Bff.Marine.Participant.Mobile.UnitTests;

/// <summary>
/// BE-MO9c — the owner notification passthrough mapping. The inbox maps to the cost-free mobile DTO: enums cross as
/// STRING names (the AdminPanel numeric-enum gotcha), the internal MetadataJson (transaction/context ids) is DROPPED,
/// and paging (Total/UnreadCount) is honoured. The preference matrix passes through (already cost-free).
/// </summary>
public sealed class MobileNotificationMapperMo9cTests
{
    private static NotificationDto Dto() => new()
    {
        Id            = 141,
        Type          = NotificationType.DisputeResolved,
        Channel       = NotificationChannel.InApp,
        Status        = NotificationStatus.Sent,
        Title         = "İtiraz çözüldü",
        Body          = "Talep #9011 için itiraz çözüldü.",
        MetadataJson  = "{\"transactionId\":123,\"contextType\":\"ServiceRequest\",\"contextId\":9011}",
        ReferenceType = "ServiceRequest",
        ReferenceId   = 9011,
        IsRead        = false,
        CreatedAt     = DateTimeOffset.UtcNow,
        ReadAt        = null,
    };

    [Fact]
    public void MapNotification_is_cost_free_and_enum_is_a_string()
    {
        var m = MobileNotificationMapper.MapNotification(Dto());

        m.Id.Should().Be(141);
        m.Type.Should().Be("DisputeResolved", "the client gets a stable enum NAME, not a number");
        m.Title.Should().Be("İtiraz çözüldü");
        m.Body.Should().Be("Talep #9011 için itiraz çözüldü.");
        m.ReferenceType.Should().Be("ServiceRequest");
        m.ReferenceId.Should().Be(9011);
        m.IsRead.Should().BeFalse();

        // MetadataJson / Channel / Status are dropped — the mobile DTO has no such members.
        typeof(MobileNotificationDto).GetProperty("MetadataJson").Should().BeNull();
        typeof(MobileNotificationDto).GetProperty("Channel").Should().BeNull();
        typeof(MobileNotificationDto).GetProperty("Status").Should().BeNull();
    }

    [Fact]
    public void MapList_honours_paging_and_counts()
    {
        var resp = new NotificationListResponse { Items = { Dto(), Dto() }, Total = 7, UnreadCount = 3 };

        var list = MobileNotificationMapper.MapList(resp);

        list.Items.Should().HaveCount(2);
        list.Total.Should().Be(7);
        list.UnreadCount.Should().Be(3);
    }

    [Fact]
    public void MapPreferences_passes_the_matrix_through()
    {
        var resp = new NotificationPreferencesResponse
        {
            Categories =
            {
                new NotificationCategoryPreferenceDto
                {
                    Category = "Payments",
                    InApp = new ChannelPreferenceDto { Enabled = true,  Locked = true  },
                    Push  = new ChannelPreferenceDto { Enabled = true,  Locked = false },
                    Email = new ChannelPreferenceDto { Enabled = false, Locked = false },
                },
            },
        };

        var m = MobileNotificationMapper.MapPreferences(resp);

        var cat = m.Categories.Should().ContainSingle().Subject;
        cat.Category.Should().Be("Payments");
        cat.InApp.Enabled.Should().BeTrue();
        cat.InApp.Locked.Should().BeTrue("InApp is always locked");
        cat.Push.Enabled.Should().BeTrue();
        cat.Push.Locked.Should().BeFalse();
        cat.Email.Enabled.Should().BeFalse();
    }

    [Fact]
    public void Mobile_notification_dto_is_cost_free()
    {
        var names = typeof(MobileNotificationDto).GetProperties().Select(p => p.Name);

        names.Should().NotContain(x =>
            x.Contains("Amount", StringComparison.OrdinalIgnoreCase) ||
            x.Contains("Commission", StringComparison.OrdinalIgnoreCase) ||
            x.Contains("Net", StringComparison.OrdinalIgnoreCase) ||
            x.Contains("Margin", StringComparison.OrdinalIgnoreCase) ||
            x.Contains("Metadata", StringComparison.OrdinalIgnoreCase) ||
            x.Contains("Provider", StringComparison.OrdinalIgnoreCase));
    }
}
