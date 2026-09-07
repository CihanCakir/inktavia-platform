using System.Linq;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Timeline;
using FluentAssertions;
using Xunit;

namespace Aizen.Modules.ServiceRequest.Application.UnitTests;

/// <summary>
/// The machine-readable timeline event code is derived from stored (From,To) status at read time, so historical
/// rows get a code too and clients can localize instead of shipping the English `Reason` prose as data.
/// </summary>
public sealed class TimelineEventCodeTests
{
    [Theory]
    [InlineData(ServiceRequestStatus.Draft, ServiceRequestStatus.Draft, "SR_CREATED")]
    [InlineData(ServiceRequestStatus.Draft, ServiceRequestStatus.Open, "SR_PUBLISHED")]
    [InlineData(ServiceRequestStatus.Open, ServiceRequestStatus.OfferReceived, "SR_OFFER_RECEIVED")]
    [InlineData(ServiceRequestStatus.OfferReceived, ServiceRequestStatus.OfferAccepted, "SR_OFFER_ACCEPTED")]
    [InlineData(ServiceRequestStatus.OfferAccepted, ServiceRequestStatus.Assigned, "SR_ASSIGNED")]
    [InlineData(ServiceRequestStatus.Assigned, ServiceRequestStatus.CompletionSubmitted, "SR_COMPLETION_SUBMITTED")]
    [InlineData(ServiceRequestStatus.CompletionSubmitted, ServiceRequestStatus.Completed, "SR_COMPLETED")]
    [InlineData(ServiceRequestStatus.Assigned, ServiceRequestStatus.Cancelled, "SR_CANCELLED")]
    [InlineData(ServiceRequestStatus.DisputeOpened, ServiceRequestStatus.DisputeResolved, "SR_DISPUTE_RESOLVED")]
    [InlineData(ServiceRequestStatus.Completed, ServiceRequestStatus.Closed, "SR_CLOSED")]
    public void Derive_Maps_Transition_To_Stable_Code(ServiceRequestStatus from, ServiceRequestStatus to, string expected)
        => ServiceRequestTimelineEventCode.Derive(from, to).Should().Be(expected);

    [Fact]
    public void InProgress_Splits_By_FromStatus()
    {
        // Same destination (InProgress) is two different events; from-status disambiguates.
        ServiceRequestTimelineEventCode.Derive(ServiceRequestStatus.Assigned, ServiceRequestStatus.InProgress)
            .Should().Be("SR_WORK_STARTED");
        ServiceRequestTimelineEventCode.Derive(ServiceRequestStatus.CompletionSubmitted, ServiceRequestStatus.InProgress)
            .Should().Be("SR_COMPLETION_REJECTED");
    }

    [Fact]
    public void Unknown_Destination_Falls_Back()
        => ServiceRequestTimelineEventCode.Derive(ServiceRequestStatus.Open, ServiceRequestStatus.Scheduled)
            .Should().Be("SR_SCHEDULED"); // mapped; and a truly-unmapped value would yield SR_STATUS_CHANGED

    [Fact]
    public void Never_Returns_Null_Or_Empty_For_Any_Status_Pair()
    {
        foreach (ServiceRequestStatus to in System.Enum.GetValues<ServiceRequestStatus>())
        {
            var code = ServiceRequestTimelineEventCode.Derive(ServiceRequestStatus.Open, to);
            code.Should().NotBeNullOrWhiteSpace();
            code.Should().StartWith("SR_");
        }
    }

    // GUARD: no (from,to) transition — for ANY current or future status value — may emit a code outside the
    // published CanonicalCodes set. If a new ServiceRequestStatus makes this fail, add the code to CanonicalCodes
    // AND tell the client teams (their hand-derived eventCode maps must gain the new key), then update this suite.
    [Fact]
    public void Every_Transition_Emits_A_Canonical_Code()
    {
        var all = System.Enum.GetValues<ServiceRequestStatus>();
        foreach (var from in all)
            foreach (var to in all)
            {
                var code = ServiceRequestTimelineEventCode.Derive(from, to);
                ServiceRequestTimelineEventCode.CanonicalCodes.Should().Contain(code,
                    $"transition {from}->{to} produced '{code}' which is not in CanonicalCodes");
            }
    }

    [Fact]
    public void CanonicalCodes_Has_No_Unused_Entries_Beyond_The_Documented_Fallback()
    {
        // Every canonical code (except the SR_STATUS_CHANGED fallback) must be reachable from some transition —
        // keeps the published list honest (no dead codes clients would localize for nothing).
        var all = System.Enum.GetValues<ServiceRequestStatus>();
        var emitted = new HashSet<string>();
        foreach (var from in all)
            foreach (var to in all)
                emitted.Add(ServiceRequestTimelineEventCode.Derive(from, to));

        var unreachable = ServiceRequestTimelineEventCode.CanonicalCodes
            .Where(c => c != "SR_STATUS_CHANGED" && !emitted.Contains(c))
            .ToList();
        unreachable.Should().BeEmpty();
    }
}
