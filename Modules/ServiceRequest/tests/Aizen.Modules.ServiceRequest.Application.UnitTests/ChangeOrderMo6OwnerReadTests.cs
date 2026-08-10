using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ChangeOrder;
using Aizen.Modules.ServiceRequest.Application.Mapping;
using Aizen.Modules.ServiceRequest.Domain.Entities.ChangeOrder;
using FluentAssertions;

namespace Aizen.Modules.ServiceRequest.Application.UnitTests;

/// <summary>
/// BE_MO6 — the owner change-order READ contract (the DTO surface the mobile BFF projects to the owner). The S11
/// apply engine + entity lifecycle are covered by <see cref="ChangeOrderLifecycleTests"/>; these focus on what the
/// owner sees: the <c>ToDto()</c> projection carries the correct customer-facing numbers (direction / applied total /
/// signed effective delta / linked ids), and the confidential provider-net stays on the CO level (dropped by the
/// mobile mapper) while the per-line shape is cost-free.
/// </summary>
public sealed class ChangeOrderMo6OwnerReadTests
{
    private static ServiceChangeOrderEntity NewCo(ServiceChangeOrderDirection direction) =>
        ServiceChangeOrderEntity.Create(
            serviceRequestId: 10, acceptedOfferId: 20, sequenceNo: 1, direction: direction,
            currencyCode: "TRY", reason: "extra work", proposedByUserId: 99,
            items: new[] { ServiceChangeOrderItemEntity.Create(
                ServiceRequestOfferItemType.Labor, "Labor", null, 2, 500m, "TRY", 0) },
            utcNow: DateTime.UtcNow);

    // (2) Approve Increase → the owner read reflects a NEW incremental capture: Applied, positive customer total, a
    // positive effective delta, and a linked incremental transaction + snapshot (the accepted snapshot is separate).
    [Fact]
    public void Applied_increase_projects_the_owner_read_numbers()
    {
        var co = NewCo(ServiceChangeOrderDirection.Increase);
        co.MarkCustomerApproved(DateTime.UtcNow);
        co.MarkApplied(economicsSnapshotId: 501, paymentTransactionId: 9001, customerTotal: 1200m, providerNet: 900m, DateTime.UtcNow);

        var dto = co.ToDto();

        dto.Direction.Should().Be(ServiceChangeOrderDirection.Increase);
        dto.Status.Should().Be(ServiceChangeOrderStatus.Applied);
        dto.AppliedCustomerTotal.Should().Be(1200m);
        dto.EffectiveTotalDelta.Should().Be(1200m);          // + for Increase
        dto.PaymentTransactionId.Should().Be(9001);          // NEW incremental escrow tx
        dto.EconomicsSnapshotId.Should().Be(501);            // NEW snapshot (original untouched)
        dto.RefundRecordId.Should().BeNull();
    }

    // (3) Approve Decrease → the owner read reflects a P10 refund of the delta: Applied, a NEGATIVE effective delta,
    // a linked refund record, and zero provider net.
    [Fact]
    public void Applied_decrease_projects_a_refund_of_the_delta()
    {
        var co = NewCo(ServiceChangeOrderDirection.Decrease);
        co.MarkCustomerApproved(DateTime.UtcNow);
        co.MarkAppliedAsReduction(originalTransactionId: 8001, refundRecordId: 701, refundedAmount: 300m, DateTime.UtcNow);

        var dto = co.ToDto();

        dto.Direction.Should().Be(ServiceChangeOrderDirection.Decrease);
        dto.Status.Should().Be(ServiceChangeOrderStatus.Applied);
        dto.AppliedCustomerTotal.Should().Be(300m);          // refunded amount (customer-facing)
        dto.EffectiveTotalDelta.Should().Be(-300m);          // − for Decrease
        dto.RefundRecordId.Should().Be(701);
        dto.AppliedProviderNet.Should().Be(0m);
    }

    // (5) Reject → terminal, no economics; the owner read shows Rejected + the reason, no applied amounts.
    [Fact]
    public void Rejected_change_order_is_terminal_with_no_economics()
    {
        var co = NewCo(ServiceChangeOrderDirection.Increase);
        co.Reject("not needed", DateTime.UtcNow);

        var dto = co.ToDto();

        dto.Status.Should().Be(ServiceChangeOrderStatus.Rejected);
        dto.RejectionReason.Should().Be("not needed");
        dto.AppliedCustomerTotal.Should().Be(0m);
        dto.EffectiveTotalDelta.Should().Be(0m);
        dto.PaymentTransactionId.Should().BeNull();
    }

    // (6) The per-line owner shape is cost-free — no provider cost / net / margin / funding amount on the line the
    // owner sees (the mobile mirrors this line; the eligibility flags it drops are not amounts).
    [Fact]
    public void Change_order_line_dto_is_cost_free()
    {
        var names = typeof(ServiceChangeOrderItemDto).GetProperties().Select(p => p.Name);

        names.Should().NotContain(n =>
            n.Contains("Cost", StringComparison.OrdinalIgnoreCase) ||
            n.Contains("Net", StringComparison.OrdinalIgnoreCase) ||
            n.Contains("Margin", StringComparison.OrdinalIgnoreCase) ||
            n.Contains("Funding", StringComparison.OrdinalIgnoreCase) ||
            n.Contains("Supplier", StringComparison.OrdinalIgnoreCase) ||
            n.Contains("Dealer", StringComparison.OrdinalIgnoreCase));
    }

    // (6) Confidentiality boundary: the ONE confidential amount (AppliedProviderNet) lives on the CO-level DTO — the
    // mobile mapper MUST drop it — and never leaks onto the per-line DTO. This guards the mobile drop contract.
    [Fact]
    public void Provider_net_is_confidential_and_co_level_only()
    {
        typeof(ServiceChangeOrderDto).GetProperty(nameof(ServiceChangeOrderDto.AppliedProviderNet))
            .Should().NotBeNull("the module CO DTO carries provider net — the mobile owner DTO must drop it");

        typeof(ServiceChangeOrderItemDto).GetProperty("AppliedProviderNet")
            .Should().BeNull("provider net must never appear on the per-line DTO");
    }
}
