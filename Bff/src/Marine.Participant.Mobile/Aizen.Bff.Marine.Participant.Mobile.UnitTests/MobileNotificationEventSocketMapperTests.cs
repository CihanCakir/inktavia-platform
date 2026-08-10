using Aizen.Bff.Marine.Participant.Mobile.Realtime;
using Aizen.Core.Realtime.Abstraction.Models;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Abstraction.Message;
using FluentAssertions;

namespace Aizen.Bff.Marine.Participant.Mobile.UnitTests;

/// <summary>
/// BE-MO9b — the owner notification-bell mapper: an in-app <see cref="NotificationSentMessage"/> for a real recipient
/// maps to the recipient's per-user group + a thin, cost-free frame; Push/Email skip; recipient≤0 skips; both the raw
/// and the <c>EventDto</c>-wrapped forms resolve. Filtering drops at <c>Map</c> (returning null), never via empty
/// targets (which would broadcast).
/// </summary>
public sealed class MobileNotificationEventSocketMapperTests
{
    private static readonly MobileNotificationEventSocketMapper Mapper = new();

    private static NotificationSentMessage Notification(
        NotificationChannel channel = NotificationChannel.InApp, long recipient = 100011) => new()
    {
        NotificationId  = 555,
        RecipientUserId = recipient,
        Type            = (NotificationType)111,   // e.g. OfferAccepted
        Channel         = channel,
        Status          = NotificationStatus.Sent,
        Title           = "Teklifiniz kabul edildi",
        SentAt          = DateTimeOffset.UtcNow,
        ReferenceType   = "ServiceRequest",
        ReferenceId     = 9011,
    };

    // (2) InApp + recipient>0 → correct group + thin cost-free frame.
    [Fact]
    public void InApp_notification_maps_to_the_recipient_group_and_thin_frame()
    {
        var n = Notification();

        var frame = Mapper.Map(n);

        frame.Should().NotBeNull();
        frame!.Type.Should().Be("mobileNotification");
        frame.Stream.Should().Be("mobile-notification:100011");
        frame.Stream.Should().Be(MobileRealtimeHub.UserGroup(n.RecipientUserId));
        frame.AggregateId.Should().Be("555");

        var payload = frame.Payload.Should().BeOfType<MobileRealtimeNotification>().Subject;
        payload.NotificationId.Should().Be(555);
        payload.Title.Should().Be("Teklifiniz kabul edildi");
        payload.ReferenceType.Should().Be("ServiceRequest");
        payload.ReferenceId.Should().Be(9011);

        var (userIds, groups) = Mapper.GetTargets(n);
        userIds.Should().BeEmpty();
        groups.Should().ContainSingle().Which.Should().Be("mobile-notification:100011");
    }

    // Push is MO9a's job — the bell (in-app surface) must skip it. Skip = null Map + empty targets.
    [Theory]
    [InlineData(NotificationChannel.Push)]
    [InlineData(NotificationChannel.Email)]
    [InlineData(NotificationChannel.Sms)]
    public void Non_inapp_channels_skip(NotificationChannel channel)
    {
        var n = Notification(channel: channel);

        Mapper.Map(n).Should().BeNull("only the in-app channel drives the bell");
        var (userIds, groups) = Mapper.GetTargets(n);
        userIds.Should().BeEmpty();
        groups.Should().BeEmpty("an empty target set here means SKIP — Map already returned null");
    }

    [Fact]
    public void Non_positive_recipient_skips()
    {
        Mapper.Map(Notification(recipient: 0)).Should().BeNull();
        Mapper.Map(Notification(recipient: -5)).Should().BeNull();
    }

    // The generic realtime consumer wraps the bus message in an EventDto — both forms must resolve identically.
    [Fact]
    public void Both_raw_and_eventdto_wrapped_forms_resolve()
    {
        var n = Notification();
        var wrapped = new EventDto { Type = nameof(NotificationSentMessage), Data = n, AggregateId = "555" };

        Mapper.Map(wrapped).Should().NotBeNull();
        Mapper.Map(wrapped)!.Stream.Should().Be("mobile-notification:100011");
        Mapper.GetTargets(wrapped).GroupNames.Should().ContainSingle().Which.Should().Be("mobile-notification:100011");
    }

    [Fact]
    public void Unknown_event_and_wrapped_unknown_skip()
    {
        Mapper.Map(new object()).Should().BeNull();
        Mapper.Map(new EventDto { Type = "Other", Data = "not a notification" }).Should().BeNull();
    }

    // Cost-free by construction: the frame payload carries no economics / provider identity.
    [Fact]
    public void Frame_payload_is_cost_free()
    {
        var names = typeof(MobileRealtimeNotification).GetProperties().Select(p => p.Name);

        names.Should().NotContain(x =>
            x.Contains("Amount", StringComparison.OrdinalIgnoreCase) ||
            x.Contains("Commission", StringComparison.OrdinalIgnoreCase) ||
            x.Contains("Net", StringComparison.OrdinalIgnoreCase) ||
            x.Contains("Margin", StringComparison.OrdinalIgnoreCase) ||
            x.Contains("Provider", StringComparison.OrdinalIgnoreCase) ||
            x.Contains("Price", StringComparison.OrdinalIgnoreCase));
    }
}
