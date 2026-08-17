using Aizen.Bff.Marine.Participant.Mobile.Application.Chat;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Chat;
using Aizen.Modules.Messaging.Abstraction.Response.Messaging;
using Aizen.Modules.ServiceRequest.Abstraction.Dto;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using FluentAssertions;

namespace Aizen.Bff.Marine.Participant.Mobile.UnitTests;

/// <summary>
/// BE-MO10a — the owner chat mapping. The inbox maps to the cost-free conversation DTO (ServiceRequestId from the
/// Messaging ContextId, ChannelOpen from status); a thread maps each Messaging message with IsOwn (SenderRole=="Owner"),
/// enums as string names, the counterparty (provider) name from participants, and SYSTEM messages carried through for
/// lifecycle pills; the SR send-result maps to the same message DTO with SenderType=Owner ⇒ IsOwn.
/// </summary>
public sealed class MobileChatMapperMo10aTests
{
    // (2) inbox → cost-free conversation DTO.
    [Fact]
    public void MapConversationList_maps_context_id_and_channel_open()
    {
        var resp = new GetConversationListResponse(
            new List<ConversationSummaryDto>
            {
                new() { Id = "c1", Title = "Motor arızası", ContextType = "ServiceRequest", ContextId = "9011",
                        Preview = "Merhaba", Timestamp = DateTimeOffset.UtcNow, UnreadCount = 2, Status = "Open" },
                new() { Id = "c2", Title = "Kapalı", ContextType = "ServiceRequest", ContextId = "9012",
                        Preview = "", Timestamp = DateTimeOffset.UtcNow, UnreadCount = 0, Status = "Closed" },
            },
            total: 2);

        var m = MobileChatMapper.MapConversationList(resp);

        m.Total.Should().Be(2);
        var first = m.Items[0];
        first.ServiceRequestId.Should().Be(9011);
        first.Title.Should().Be("Motor arızası");
        first.LastMessagePreview.Should().Be("Merhaba");
        first.UnreadCount.Should().Be(2);
        first.ChannelOpen.Should().BeTrue();
        m.Items[1].ChannelOpen.Should().BeFalse("a Closed conversation channel is not open");
        m.Items[1].LastMessagePreview.Should().BeNull("an empty preview maps to null");
    }

    // (3)(5) thread → messages with IsOwn, string enums, counterparty name, System carried through.
    [Fact]
    public void MapThread_computes_isOwn_counterparty_and_carries_system()
    {
        var detail = new ConversationDetailDto
        {
            Id = "c1", Title = "Motor arızası", ContextType = "ServiceRequest", ContextId = "9011", Status = "Open",
            Participants = new List<ParticipantDto>
            {
                new("100011", "OWNER 1", "Owner"),
                new("100022", "PROVIDER 2 AS", "Provider"),
            },
            Messages = new List<ChatMessageDto>
            {
                new() { Id = "1", SenderUserId = "100011", SenderName = "OWNER 1", SenderRole = "Owner",
                        Content = "Merhaba", Type = "Text", Timestamp = DateTimeOffset.UtcNow },
                new() { Id = "2", SenderUserId = "100022", SenderName = "PROVIDER 2 AS", SenderRole = "Provider",
                        Content = "Yola çıktım", Type = "Text", Timestamp = DateTimeOffset.UtcNow },
                new() { Id = "3", SenderUserId = "0", SenderName = "System", SenderRole = "System",
                        Content = "Teklif kabul edildi", Type = "StatusChange", Timestamp = DateTimeOffset.UtcNow },
            },
        };

        var t = MobileChatMapper.MapThread(new GetConversationDetailResponse(detail));

        t.ServiceRequestId.Should().Be(9011);
        t.ChannelOpen.Should().BeTrue();
        t.CounterpartyName.Should().Be("PROVIDER 2 AS", "the counterparty is the Provider participant");
        t.Messages.Should().HaveCount(3);
        t.Messages[0].IsOwn.Should().BeTrue();
        t.Messages[0].SenderType.Should().Be("Owner");
        t.Messages[1].IsOwn.Should().BeFalse();
        t.Messages[2].SenderType.Should().Be("System", "System lifecycle messages ride through for pills");
        t.Messages[2].IsOwn.Should().BeFalse();
    }

    // (4) SR send-result → the same message DTO; an owner send is IsOwn, enums as strings.
    [Fact]
    public void MapSentMessage_maps_owner_send()
    {
        var sent = new ServiceRequestMessageDto
        {
            Id = 55, SenderUserId = 100011, SenderType = ServiceRequestMessageSenderType.Owner,
            MessageType = ServiceRequestMessageType.Text, Content = "Teşekkürler", IsRead = false,
            CreatedAt = DateTime.UtcNow,
        };

        var m = MobileChatMapper.MapSentMessage(sent);

        m.Id.Should().Be(55);
        m.SenderType.Should().Be("Owner");
        m.IsOwn.Should().BeTrue();
        m.MessageType.Should().Be("Text");
        m.Content.Should().Be("Teşekkürler");
    }

    // (6) cost-free: the chat DTOs carry no economics.
    [Fact]
    public void Chat_dtos_are_cost_free()
    {
        var names = typeof(MobileChatMessageDto).GetProperties().Select(p => p.Name)
            .Concat(typeof(MobileConversationDto).GetProperties().Select(p => p.Name));

        names.Should().NotContain(x =>
            x.Contains("Amount", StringComparison.OrdinalIgnoreCase) ||
            x.Contains("Commission", StringComparison.OrdinalIgnoreCase) ||
            x.Contains("Net", StringComparison.OrdinalIgnoreCase) ||
            x.Contains("Margin", StringComparison.OrdinalIgnoreCase) ||
            x.Contains("Price", StringComparison.OrdinalIgnoreCase));
    }
}
