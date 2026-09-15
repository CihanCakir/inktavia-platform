using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Domain.Entities;
using FluentAssertions;

namespace Aizen.Modules.CargoDry.Application.UnitTests;

/// <summary>Wave 4A — stock-request lifecycle (Pending → Approved → Shipped → Received) transition guards.</summary>
public sealed class CargoDryStockRequestLifecycleTests
{
    private static CargoDryStockRequestEntity NewPending()
        => CargoDryStockRequestEntity.Create(providerProfileId: 100011, productCode: "CD-MARINE-PRO",
            requestedQuantity: 5, agreementId: null, note: null);

    [Fact]
    public void Create_starts_Pending()
        => NewPending().Status.Should().Be(CargoDryStockRequestStatus.Pending);

    [Fact]
    public void Approve_then_Ship_then_Receive_walks_the_happy_path()
    {
        var e = NewPending();
        var deadline = new DateTimeOffset(2026, 9, 22, 0, 0, 0, TimeSpan.Zero);

        e.Approve(userId: 9, note: "ok", batchCode: "BATCH-1", allocatedQty: 5);
        e.Status.Should().Be(CargoDryStockRequestStatus.Approved);
        e.ApprovedBatchCode.Should().Be("BATCH-1");
        e.AllocatedQuantity.Should().Be(5);

        e.Ship(userId: 9, trackingCode: "TRK-123", autoReceiveDeadlineUtc: deadline);
        e.Status.Should().Be(CargoDryStockRequestStatus.Shipped);
        e.TrackingCode.Should().Be("TRK-123");
        e.AutoReceiveDeadlineUtc.Should().Be(deadline);
        e.ShippedAtUtc.Should().NotBeNull();

        e.Receive(receivedByUserId: null); // auto/system path
        e.Status.Should().Be(CargoDryStockRequestStatus.Received);
        e.ReceivedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void Ship_requires_Approved()
    {
        var e = NewPending(); // Pending
        var act = () => e.Ship(1, "TRK", DateTimeOffset.UtcNow.AddDays(7));
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Ship_requires_a_tracking_code()
    {
        var e = NewPending();
        e.Approve(1, null, "B", 1);
        var act = () => e.Ship(1, "  ", DateTimeOffset.UtcNow.AddDays(7));
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Receive_requires_Shipped()
    {
        var e = NewPending();
        e.Approve(1, null, "B", 1); // Approved, not Shipped
        var act = () => e.Receive(null);
        act.Should().Throw<InvalidOperationException>();
    }
}
