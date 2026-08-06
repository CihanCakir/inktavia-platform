using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Application.Mapping;
using FluentAssertions;
using PayEnum = Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Application.UnitTests;

/// <summary>
/// BE-S13b — the SR half of the dispute-outcome → refund chain (outcome → RefundReason + routing flags). The Payment
/// half (RefundReason → RefundCause) is asserted by <c>RefundCauseMapTests</c> in the Payment.Domain unit tests, and
/// the amount ≤ refundable guard by <c>DisputeRefundGuardTests</c>.
/// </summary>
public sealed class DisputeOutcomeRefundMapTests
{
    [Theory]
    [InlineData(DisputeResolutionOutcome.FavorPayerFullRefund)]
    [InlineData(DisputeResolutionOutcome.FavorPayerPartialRefund)]
    [InlineData(DisputeResolutionOutcome.Split)]
    public void Payer_favoured_outcomes_map_to_dispute_resolved_for_payer(DisputeResolutionOutcome outcome)
        => DisputeOutcomeRefundMap.ToRefundReason(outcome)
            .Should().Be(PayEnum.RefundReason.DisputeResolvedForPayer);

    [Fact]
    public void FavorProviderRelease_releases_escrow_and_is_not_a_refund()
    {
        DisputeOutcomeRefundMap.ReleasesEscrow(DisputeResolutionOutcome.FavorProviderRelease).Should().BeTrue();
        DisputeOutcomeRefundMap.IsFullRefund(DisputeResolutionOutcome.FavorProviderRelease).Should().BeFalse();
        DisputeOutcomeRefundMap.RequiresAmount(DisputeResolutionOutcome.FavorProviderRelease).Should().BeFalse();
    }

    [Fact]
    public void FullRefund_takes_the_whole_refundable_and_needs_no_amount()
    {
        DisputeOutcomeRefundMap.IsFullRefund(DisputeResolutionOutcome.FavorPayerFullRefund).Should().BeTrue();
        DisputeOutcomeRefundMap.RequiresAmount(DisputeResolutionOutcome.FavorPayerFullRefund).Should().BeFalse();
        DisputeOutcomeRefundMap.ReleasesEscrow(DisputeResolutionOutcome.FavorPayerFullRefund).Should().BeFalse();
    }

    [Theory]
    [InlineData(DisputeResolutionOutcome.FavorPayerPartialRefund)]
    [InlineData(DisputeResolutionOutcome.Split)]
    public void Partial_and_split_require_an_explicit_amount(DisputeResolutionOutcome outcome)
    {
        DisputeOutcomeRefundMap.RequiresAmount(outcome).Should().BeTrue();
        DisputeOutcomeRefundMap.IsFullRefund(outcome).Should().BeFalse();
        DisputeOutcomeRefundMap.ReleasesEscrow(outcome).Should().BeFalse();
    }
}
