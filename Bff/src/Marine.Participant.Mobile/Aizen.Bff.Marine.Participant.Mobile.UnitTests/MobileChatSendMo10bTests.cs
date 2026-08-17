using Aizen.Bff.Marine.Participant.Mobile.Application.Chat;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Chat;
using Aizen.Core.Infrastructure.Exception;
using FluentAssertions;

namespace Aizen.Bff.Marine.Participant.Mobile.UnitTests;

/// <summary>
/// BE-MO10b — the owner chat send validation: exactly one of { text, image, location }, coords in range, length caps.
/// The SR module validates none of this, so the BFF is the gate.
/// </summary>
public sealed class MobileChatSendMo10bTests
{
    // (2) text.
    [Fact]
    public void Text_only_is_a_text_message()
    {
        var (kind, content) = MobileChatSend.Validate("  merhaba  ", null, null, null, null);
        kind.Should().Be(MobileChatSendKind.Text);
        content.Should().Be("merhaba", "content is trimmed");
    }

    // (2) image.
    [Fact]
    public void Attachment_only_is_an_image_message()
    {
        var (kind, content) = MobileChatSend.Validate(null, Guid.NewGuid(), null, null, null);
        kind.Should().Be(MobileChatSendKind.Image);
        content.Should().BeEmpty();
    }

    // (4) location.
    [Fact]
    public void Coords_only_is_a_location_message()
    {
        var (kind, _) = MobileChatSend.Validate(null, null, 41.0, 29.0, "Marina");
        kind.Should().Be(MobileChatSendKind.Location);
    }

    // exactly-one-of: none → reject.
    [Fact]
    public void Empty_message_is_rejected()
    {
        var act = () => MobileChatSend.Validate("   ", null, null, null, null);
        act.Should().Throw<AizenBusinessException>().WithMessage("*text, an image, or a location*");
    }

    // exactly-one-of: more than one → reject.
    [Fact]
    public void Text_plus_image_is_rejected()
    {
        var act = () => MobileChatSend.Validate("hi", Guid.NewGuid(), null, null, null);
        act.Should().Throw<AizenBusinessException>().WithMessage("*only one*");
    }

    [Theory]
    [InlineData(91.0, 29.0)]   // lat out of range
    [InlineData(41.0, 181.0)]  // lng out of range
    public void Out_of_range_coords_are_rejected(double lat, double lng)
    {
        var act = () => MobileChatSend.Validate(null, null, lat, lng, null);
        act.Should().Throw<AizenBusinessException>();
    }

    [Fact]
    public void Overlong_text_is_rejected()
    {
        var act = () => MobileChatSend.Validate(new string('x', MobileChatSend.MaxContentLength + 1), null, null, null, null);
        act.Should().Throw<AizenBusinessException>().WithMessage("*exceed*");
    }

    // (6) the extended send request is cost-free.
    [Fact]
    public void Send_request_is_cost_free()
    {
        var names = typeof(MobileSendChatMessageRequest).GetProperties().Select(p => p.Name);
        names.Should().NotContain(x =>
            x.Contains("Amount", StringComparison.OrdinalIgnoreCase) ||
            x.Contains("Commission", StringComparison.OrdinalIgnoreCase) ||
            x.Contains("Price", StringComparison.OrdinalIgnoreCase));
    }
}
