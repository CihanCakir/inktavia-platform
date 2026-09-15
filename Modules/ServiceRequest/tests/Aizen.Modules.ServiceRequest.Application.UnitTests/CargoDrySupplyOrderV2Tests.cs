using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Core.Realtime.Abstraction.Interfaces;
using Aizen.Modules.CargoDry.Abstraction.Message;
using Aizen.Modules.CargoDry.Abstraction.RemoteCall;
using Aizen.Modules.Payment.Abstraction.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Application.Command.Offer.CreateCargoDrySupplyOffer;
using Aizen.Modules.ServiceRequest.Application.Command.ServiceRequest.CompleteCargoDrySupplyOrder;
using Aizen.Modules.ServiceRequest.Application.Realtime;
using Aizen.Modules.ServiceRequest.Application.Services;
using Aizen.Modules.ServiceRequest.Domain.Entities.ServiceRequest;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Aizen.Core.Infrastructure.Exception;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Aizen.Modules.ServiceRequest.Application.UnitTests;

/// <summary>
/// CargoDry supply v2 — order-model edges: cargo-fallback race (deterministic winner), delivered set-once, cargo ship
/// transition, and auto-complete idempotency vs a late QR scan (terminal completion is a no-op — no double effects).
/// </summary>
public sealed class CargoDrySupplyOrderV2Tests
{
    private static ServiceRequestEntity NewSupplyOrder()
    {
        var sr = ServiceRequestEntity.Create(
            "SR1", ownerUserId: 1, vesselId: 2, serviceCategoryCode: "CARGODRY_SUPPLY", serviceTypeCode: null,
            title: "CargoDry", description: null, priority: ServiceRequestPriority.Normal,
            requestedStartDate: null, requestedEndDate: null, locationCountryCode: null, locationCityCode: null,
            locationMarinaName: null, locationLatitude: null, locationLongitude: null, ownerNotes: null, expiresAt: null);
        sr.SetCargoDryProductCode("STANDARD-90");
        sr.SetCargoDryRetail(149.99m, "TRY");
        return sr;
    }

    // ── D4 window race: fallback only from an open state; a provider accept that already assigned wins. ──
    [Fact]
    public void Fallback_only_transitions_from_open_deterministic_winner()
    {
        var open = NewSupplyOrder();
        open.Publish(); // Open
        open.MoveToAwaitingShipment();
        open.Status.Should().Be(ServiceRequestStatus.AwaitingShipment);

        var assigned = NewSupplyOrder();
        assigned.Publish();
        assigned.Assign(99, "Provider X"); // provider accept won → Assigned
        var act = () => assigned.MoveToAwaitingShipment();
        act.Should().Throw<InvalidOperationException>("a provider that already accepted wins the race — no cargo fallback");
    }

    // ── D3 mark-delivered: only from Assigned, set-once. ──
    [Fact]
    public void MarkDelivered_only_from_assigned_and_set_once()
    {
        var sr = NewSupplyOrder();
        sr.Publish();
        var tooEarly = () => sr.MarkDelivered(10, DateTime.UtcNow, DateTime.UtcNow.AddHours(24));
        tooEarly.Should().Throw<InvalidOperationException>();

        sr.Assign(99, "Provider X");
        sr.MarkDelivered(10, DateTime.UtcNow, DateTime.UtcNow.AddHours(24));
        sr.DeliveredKitId.Should().Be(10);
        sr.DeliveredAtUtc.Should().NotBeNull();

        var again = () => sr.MarkDelivered(11, DateTime.UtcNow, DateTime.UtcNow.AddHours(24));
        again.Should().Throw<InvalidOperationException>("delivery is set-once");
    }

    // ── D4 cargo ship: only from AwaitingShipment. ──
    [Fact]
    public void MarkCargoShipped_only_from_awaiting_shipment()
    {
        var sr = NewSupplyOrder();
        sr.Publish();
        var wrong = () => sr.MarkCargoShipped("TRACK1", DateTime.UtcNow, DateTime.UtcNow.AddDays(7));
        wrong.Should().Throw<InvalidOperationException>();

        sr.MoveToAwaitingShipment();
        sr.MarkCargoShipped("TRACK1", DateTime.UtcNow, DateTime.UtcNow.AddDays(7), shippedKitId: 55);
        sr.Status.Should().Be(ServiceRequestStatus.Shipped);
        sr.TrackingCode.Should().Be("TRACK1");
        sr.DeliveredKitId.Should().Be(55);
    }

    // ── D3 auto-complete idempotency: a completion on an already-terminal order is a no-op (no messages). ──
    [Fact]
    public async Task Complete_is_idempotent_noop_on_terminal_order_late_qr_safe()
    {
        var sr = NewSupplyOrder();
        sr.Assign(99, "Provider X");
        sr.MarkDelivered(10, DateTime.UtcNow, DateTime.UtcNow.AddHours(24));
        sr.MarkCompleted(DateTimeOffset.UtcNow);
        sr.ReleasePayment(); // → Closed (already completed, e.g. by an earlier sweep)

        var repo = Substitute.For<IServiceRequestRepository>();
        repo.GetByIdWithDetailsAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(sr);
        var publisher = Substitute.For<IAizenMessagePublisher>();

        var handler = new CompleteCargoDrySupplyOrderCommandHandler(
            repo, publisher, NullLogger<CompleteCargoDrySupplyOrderCommandHandler>.Instance);

        var result = await handler.Handle(new CompleteCargoDrySupplyOrderCommand { ServiceRequestId = 1 }, CancellationToken.None);

        result!.Completed.Should().BeFalse("already terminal — a late QR scan / re-run must not re-complete");
        await publisher.DidNotReceive().PublishAsync(Arg.Any<ServiceRequestCompletedMessage>(), Arg.Any<CancellationToken>());
        await publisher.DidNotReceive().PublishAsync(Arg.Any<CargoDrySupplyOrderCompletedMessage>(), Arg.Any<CancellationToken>());
    }

    // ── Provider auto-complete happy path: completes + publishes both messages exactly once. ──
    [Fact]
    public async Task Complete_provider_path_publishes_release_and_record_messages()
    {
        var sr = NewSupplyOrder();
        sr.Assign(99, "Provider X");
        sr.MarkDelivered(10, DateTime.UtcNow, DateTime.UtcNow.AddHours(24));

        var repo = Substitute.For<IServiceRequestRepository>();
        repo.GetByIdWithDetailsAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(sr);
        var publisher = Substitute.For<IAizenMessagePublisher>();

        var handler = new CompleteCargoDrySupplyOrderCommandHandler(
            repo, publisher, NullLogger<CompleteCargoDrySupplyOrderCommandHandler>.Instance);

        var result = await handler.Handle(new CompleteCargoDrySupplyOrderCommand { ServiceRequestId = 1 }, CancellationToken.None);

        result!.Completed.Should().BeTrue();
        result.IsCargoSale.Should().BeFalse();
        sr.Status.Should().Be(ServiceRequestStatus.Closed);
        await publisher.Received(1).PublishAsync(Arg.Any<ServiceRequestCompletedMessage>(), Arg.Any<CancellationToken>());
        await publisher.Received(1).PublishAsync(
            Arg.Is<CargoDrySupplyOrderCompletedMessage>(m => !m.IsCargoSale && m.DeliveredKitId == 10 && m.SaleAmount == 149.99m),
            Arg.Any<CancellationToken>());
    }

    // ── F2 — legacy in-flight guard: a supply SR created pre-v2 (no escrow at creation → PaymentTransactionId null)
    //    must be rejected on provider accept (SR_CARGODRY_LEGACY_UNPAID), never direct-assigned unpaid. ──
    [Fact]
    public async Task Provider_accept_rejects_legacy_unpaid_supply_order()
    {
        var sr = NewSupplyOrder();          // PaymentTransactionId is null (v1/legacy) — NewSupplyOrder never sets it
        sr.Publish();                        // Open
        sr.SetProviderAcceptDeadline(DateTime.UtcNow.AddHours(5)); // within window (so the guard, not the deadline, fires)
        sr.PaymentTransactionId.Should().BeNull();

        var srRepo = Substitute.For<IServiceRequestRepository>();
        srRepo.GetByIdWithDetailsAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(sr);
        var realtime = new ServiceRequestRealtimePublisher(Substitute.For<IRealtimePublisher>());
        var msg = Substitute.For<IAizenMessagePublisher>();
        var assignmentCreator = new ServiceRequestAssignmentCreator(
            srRepo, Substitute.For<IServiceRequestAssignmentRepository>(), realtime, msg);
        var cargoDry = Substitute.For<ICargoDrySupplyRemoteCall>();

        // The legacy guard fires BEFORE the handler touches the DbContext / provider identity / CargoDry, so db=null! is safe here.
        var handler = new CreateCargoDrySupplyOfferCommandHandler(
            srRepo, Substitute.For<IServiceRequestOfferRepository>(), Substitute.For<IAizenInfoAccessor>(),
            realtime, msg, db: null!, cargoDry, assignmentCreator,
            NullLogger<CreateCargoDrySupplyOfferCommandHandler>.Instance);

        var act = () => handler.Handle(new CreateCargoDrySupplyOfferCommand { ServiceRequestId = 55 }, CancellationToken.None);

        (await act.Should().ThrowAsync<AizenBusinessException>()).Which.Message.Should().Contain("SR_CARGODRY_LEGACY_UNPAID");
        await cargoDry.DidNotReceive().GetSupplyAcceptContextAsync(Arg.Any<long>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
