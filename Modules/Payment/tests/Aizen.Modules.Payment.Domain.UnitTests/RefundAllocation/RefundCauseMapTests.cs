using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.RefundAllocation;
using FluentAssertions;

namespace Aizen.Modules.Payment.Domain.UnitTests.RefundAllocation;

/// <summary>
/// N-E — the second half of the deterministic chain SR reason → RefundReason → RefundCause. Confirms the RefundReasons
/// that the N-E map emits resolve to the intended allocation causes (so the mapped SR reason drives a distinct P10
/// allocation instead of the old fixed CustomerCancelledBeforeWork).
/// </summary>
public sealed class RefundCauseMapTests
{
    [Theory]
    [InlineData(RefundReason.UserCancel,               RefundCause.CustomerCancelledBeforeWork)]
    [InlineData(RefundReason.ServiceRequestCancelled,  RefundCause.CustomerCancelledBeforeWork)]
    [InlineData(RefundReason.ProviderFailedToDeliver,  RefundCause.ProviderCancelled)]
    [InlineData(RefundReason.DuplicateCharge,          RefundCause.DuplicatePayment)]
    [InlineData(RefundReason.ServiceNotDelivered,      RefundCause.TechnicalFailure)]
    public void Mapped_refund_reason_resolves_to_expected_cause(RefundReason reason, RefundCause expected)
        => RefundCauseMap.FromReason(reason).Should().Be(expected);
}
