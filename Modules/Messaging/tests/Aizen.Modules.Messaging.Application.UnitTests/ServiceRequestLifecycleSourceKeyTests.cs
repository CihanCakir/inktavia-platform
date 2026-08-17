using Aizen.Modules.Messaging.Domain.Mapping;
using FluentAssertions;

namespace Aizen.Modules.Messaging.Application.UnitTests;

/// <summary>
/// BE_WC1 — the SourceKey convergence between the two producers of a lifecycle message. The SR→Messaging sync consumer
/// derives the key from a mirrored chat row via <see cref="ServiceRequestMessageMapping.LifecycleCode"/>; the WC1
/// Messaging consumers derive it straight from the domain event. Both MUST land on the same
/// <c>sys:{srId}:{CODE}</c> so the WC0 partial unique index keeps exactly one row during the parallel run. These tests
/// pin that mapping (the algorithmic core; the DB-level dedup itself is covered by the SQL integration check).
/// SR ints: senderType 1=Owner 2=Provider 4=System; messageType 1=Text 3=StatusChange 4=Offer 5=Image 6=Location.
/// </summary>
public sealed class ServiceRequestLifecycleSourceKeyTests
{
    [Theory]
    [InlineData(4, 3, "OFFER_ACCEPTED", "OFFER_ACCEPTED")]      // System StatusChange
    [InlineData(4, 3, "JOB_STARTED", "JOB_STARTED")]
    [InlineData(4, 3, "JOB_COMPLETED", "JOB_COMPLETED")]
    [InlineData(4, 3, "CONVERSATION_CLOSED", "CONVERSATION_CLOSED")]
    [InlineData(2, 4, "offer:5001|8100.00 TRY", "OFFER:5001")]  // Provider Offer card → OFFER:{offerId}
    [InlineData(2, 4, "offer:42|0.00 TRY", "OFFER:42")]
    public void LifecycleCode_ForLifecycleMessages_ReturnsCode(int senderType, int msgType, string content, string expected)
        => ServiceRequestMessageMapping.LifecycleCode(senderType, msgType, content).Should().Be(expected);

    [Theory]
    [InlineData(1, 1, "hello there")]      // Owner Text
    [InlineData(2, 1, "on my way")]        // Provider Text
    [InlineData(2, 5, "<image>")]          // Image
    [InlineData(1, 6, "{\"lat\":43.7}")]   // Location
    public void LifecycleCode_ForRegularChat_ReturnsNull(int senderType, int msgType, string content)
        => ServiceRequestMessageMapping.LifecycleCode(senderType, msgType, content).Should().BeNull();

    [Fact]
    public void SyncConsumer_And_Wc1Consumer_ConvergeOnSameSourceKey_ForSystemMessage()
    {
        const long srId = 101;
        // WC1 consumer side: keyed directly off the domain event's code.
        var wc1Key = ServiceRequestMessageMapping.SystemSourceKey(srId, "OFFER_ACCEPTED");
        // Sync-consumer side: derives the code from the mirrored chat row, then the SAME SystemSourceKey.
        var code = ServiceRequestMessageMapping.LifecycleCode(4, 3, "OFFER_ACCEPTED");
        var syncKey = ServiceRequestMessageMapping.SystemSourceKey(srId, code!);

        wc1Key.Should().Be("sys:101:OFFER_ACCEPTED");
        syncKey.Should().Be(wc1Key, "both producers must collapse to one row via the WC0 unique index");
    }

    [Fact]
    public void SyncConsumer_And_Wc1Consumer_ConvergeOnSameSourceKey_ForOfferCard()
    {
        const long srId = 102;
        const long offerId = 5001;
        // WC1 offer-card consumer builds the key from OfferId.
        var wc1Key = ServiceRequestMessageMapping.SystemSourceKey(srId, $"OFFER:{offerId}");
        // Sync consumer parses the mirrored "offer:{id}|{total} {ccy}" content.
        var code = ServiceRequestMessageMapping.LifecycleCode(2, 4, $"offer:{offerId}|8100.00 TRY");
        var syncKey = ServiceRequestMessageMapping.SystemSourceKey(srId, code!);

        wc1Key.Should().Be("sys:102:OFFER:5001");
        syncKey.Should().Be(wc1Key);
    }

    [Fact]
    public void RegularChat_KeysOnMessageId_NotSysKey()
    {
        // A normal owner text keys on the SR message id (distinct namespace) — never collides with a lifecycle sys: key.
        ServiceRequestMessageMapping.LifecycleCode(1, 1, "hello").Should().BeNull();
        ServiceRequestMessageMapping.SourceKey(101, 987).Should().Be("sr:101:987");
    }
}
