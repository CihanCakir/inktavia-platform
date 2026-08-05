using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Application.Mapping;
using FluentAssertions;
using PayEnum = Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Application.UnitTests;

/// <summary>
/// N-E — the SR structured reject/cancel reason → Payment RefundReason map (first half of the deterministic chain
/// SR reason → RefundReason → RefundCause → RefundAllocationPolicy). The second half (RefundReason → RefundCause) is
/// asserted by <c>RefundCauseMapTests</c> in the Payment.Domain unit tests.
/// </summary>
public sealed class NEReasonRefundMapTests
{
    [Theory]
    [InlineData(ServiceRequestCancelReason.NoLongerNeeded,       PayEnum.RefundReason.UserCancel)]
    [InlineData(ServiceRequestCancelReason.FoundAnotherProvider, PayEnum.RefundReason.UserCancel)]
    [InlineData(ServiceRequestCancelReason.PriceTooHigh,         PayEnum.RefundReason.UserCancel)]
    [InlineData(ServiceRequestCancelReason.ChangedMind,          PayEnum.RefundReason.UserCancel)]
    [InlineData(ServiceRequestCancelReason.ProviderUnresponsive, PayEnum.RefundReason.ProviderFailedToDeliver)]
    [InlineData(ServiceRequestCancelReason.Duplicate,            PayEnum.RefundReason.DuplicateCharge)]
    [InlineData(ServiceRequestCancelReason.Other,                PayEnum.RefundReason.ServiceRequestCancelled)]
    public void Cancel_reason_maps_to_expected_refund_reason(
        ServiceRequestCancelReason reason, PayEnum.RefundReason expected)
        => ServiceRequestReasonRefundMap.ToRefundReason(reason).Should().Be(expected);

    [Fact]
    public void Null_cancel_reason_falls_back_to_service_request_cancelled()
        => ServiceRequestReasonRefundMap.ToRefundReason((ServiceRequestCancelReason?)null)
            .Should().Be(PayEnum.RefundReason.ServiceRequestCancelled);

    [Theory]
    [InlineData(AssignmentRejectReason.Unavailable)]
    [InlineData(AssignmentRejectReason.OutOfServiceArea)]
    [InlineData(AssignmentRejectReason.CapacityFull)]
    [InlineData(AssignmentRejectReason.ScheduleConflict)]
    [InlineData(AssignmentRejectReason.Other)]
    public void Assignment_reject_is_always_provider_fault(AssignmentRejectReason reason)
        => ServiceRequestReasonRefundMap.ToRefundReason(reason)
            .Should().Be(PayEnum.RefundReason.ProviderFailedToDeliver);

    [Theory]
    [InlineData(CompletionRejectReason.WorkIncomplete)]
    [InlineData(CompletionRejectReason.QualityIssue)]
    [InlineData(CompletionRejectReason.NotAsAgreed)]
    [InlineData(CompletionRejectReason.Other)]
    public void Completion_reject_maps_to_service_not_delivered(CompletionRejectReason reason)
        => ServiceRequestReasonRefundMap.ToRefundReason(reason)
            .Should().Be(PayEnum.RefundReason.ServiceNotDelivered);
}
