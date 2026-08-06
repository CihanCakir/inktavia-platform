using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Application.Command.Offer;
using Aizen.Modules.ServiceRequest.Application.Services;
using Aizen.Modules.ServiceRequest.Application.Services.ChangeOrder;
using Aizen.Modules.ServiceRequest.Domain.Entities.ChangeOrder;
using Aizen.Modules.ServiceRequest.Domain.Entities.Offer;
using Aizen.Modules.ServiceRequest.Domain.Entities.ServiceRequest;
using FluentAssertions;
using PayEnum = Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Application.UnitTests;

/// <summary>
/// BE-S11b — the incremental-economics request builder reuses the shared offer calc + the P8 line mapping, but under a
/// DISTINCT change-order context ref so Payment mints a NEW snapshot + incremental escrow (never the original). Also proves
/// the FixedPrice acceptance mapping is byte-identical to pre-S11 (OfferType is inert).
/// </summary>
public sealed class ChangeOrderEconomicsMappingTests
{
    private static readonly OfferCalculationService Calc = new();

    private static ServiceRequestEntity NewSr() =>
        ServiceRequestEntity.Create(
            "SR-CODE", ownerUserId: 77, vesselId: 5, serviceCategoryCode: "ENGINE",
            serviceTypeCode: null, title: "t", description: null, priority: ServiceRequestPriority.Normal,
            requestedStartDate: null, requestedEndDate: null, locationCountryCode: null, locationCityCode: null,
            locationMarinaName: null, locationLatitude: null, locationLongitude: null, ownerNotes: null, expiresAt: null);

    private static ServiceRequestOfferEntity BuildAcceptedOffer(params ServiceRequestOfferItemEntity[] items)
    {
        var offer = ServiceRequestOfferEntity.Create(1, 2, 3, 0m, "TRY", null, null, null, null, null, null);
        foreach (var i in items) offer.AddItem(i);
        Calc.Calculate(offer);
        return offer;
    }

    private static ServiceRequestOfferItemEntity OfferItem(ServiceRequestOfferItemType type, decimal qty, decimal price, decimal tax = 0m)
        => ServiceRequestOfferItemEntity.Create(0, type, $"{type}", null, qty, price, "TRY", 0, null, tax, OfferDiscountType.None, null, PricingMethod.Fixed, null);

    private static ServiceChangeOrderItemEntity CoItem(ServiceRequestOfferItemType type, decimal qty, decimal price, int sort, decimal tax = 0m)
        => ServiceChangeOrderItemEntity.Create(type, $"{type}", null, qty, price, "TRY", sort, null, tax);

    private static ServiceChangeOrderEntity BuildChangeOrder(
        ServiceChangeOrderDirection direction, params ServiceChangeOrderItemEntity[] items)
        => ServiceChangeOrderEntity.Create(
            serviceRequestId: 0, acceptedOfferId: 0, sequenceNo: 1, direction: direction,
            currencyCode: "TRY", reason: "extra work", proposedByUserId: 99, items: items, utcNow: DateTime.UtcNow);

    // ── (1) FixedPrice regression: OfferType is inert; the acceptance request is unchanged (still SR-{}-OFFER-{}) ──
    [Fact]
    public void FixedPriceOffer_AcceptanceRequest_IsUnchanged_ByOfferType()
    {
        var sr = NewSr();
        var offer = BuildAcceptedOffer(OfferItem(ServiceRequestOfferItemType.Service, 1, 5000m));

        offer.OfferType.Should().Be(OfferType.FixedPrice, "default (and every pre-S11 row) is FixedPrice");

        var req = AcceptServiceRequestOfferCommandHandler.BuildEconomicsRequest(sr, offer);
        req.IdempotencyKey.Should().Be($"SR-{sr.Id}-OFFER-{offer.Id}", "acceptance key must have NO change-order suffix");
        req.Lines.Should().ContainSingle();
        req.Lines[0].LineProviderRevenue.Should().Be(5000m);
    }

    // ── (3) Increase apply builds the INCREMENTAL request from CO lines under a DISTINCT context ref ──
    [Fact]
    public void IncrementalRequest_UsesChangeOrderContextRef_AndMapsCoLines()
    {
        var sr = NewSr();
        var offer = BuildAcceptedOffer(OfferItem(ServiceRequestOfferItemType.Service, 1, 5000m));
        var originalGrand = offer.GrandTotal;

        var co = BuildChangeOrder(ServiceChangeOrderDirection.Increase,
            CoItem(ServiceRequestOfferItemType.Labor, 2, 500m, 0),      // 1000
            CoItem(ServiceRequestOfferItemType.Travel, 1, 300m, 1));    // 300 exempt

        var req = ServiceChangeOrderEconomics.BuildIncrementalEconomicsRequest(sr, offer, co, Calc);

        req.IdempotencyKey.Should().Be($"SR-{sr.Id}-OFFER-{offer.Id}-CO-{co.Id}");
        req.IdempotencyKey.Should().NotBe($"SR-{sr.Id}-OFFER-{offer.Id}", "the CO must not reuse the acceptance key");
        req.Lines.Should().HaveCount(2);
        req.Lines.Select(l => l.LineRef).Should().OnlyHaveUniqueItems("each transient line needs a distinct LineRef");

        var labor = req.Lines.Single(l => l.ItemType == (int)ServiceRequestOfferItemType.Labor);
        labor.LineProviderRevenue.Should().Be(1000m);
        labor.CommissionEligibility.Should().Be(PayEnum.LineCommissionEligibility.Eligible);
        var travel = req.Lines.Single(l => l.ItemType == (int)ServiceRequestOfferItemType.Travel);
        travel.CommissionBaseAmount.Should().Be(0m, "travel is commission-exempt");

        // The accepted offer (the source of the immutable acceptance snapshot) is NOT touched by building the increment.
        offer.GrandTotal.Should().Be(originalGrand);
        offer.Items.Should().ContainSingle("the change order never mutates the accepted offer's lines");
    }

    // ── (6) Reduction amount = the CO lines' grand total; the P10 context ref matches the increase ref ──
    [Fact]
    public void ReductionAmount_IsLineGrandTotal_AndSharesTheChangeOrderContextRef()
    {
        var offer = BuildAcceptedOffer(OfferItem(ServiceRequestOfferItemType.Service, 1, 5000m));
        var co = BuildChangeOrder(ServiceChangeOrderDirection.Decrease,
            CoItem(ServiceRequestOfferItemType.Service, 1, 1200m, 0, tax: 0.20m));   // 1200 + 240 vat = 1440

        var reduction = ServiceChangeOrderEconomics.ComputeLineGrandTotal(offer, co, Calc);
        reduction.Should().Be(1440m);

        // The reduction (P10) and the increase (P8) share the SAME change-order context ref for idempotency; the Payment
        // side (ApplyChangeOrderReductionCommandHandler.ContextRef) mirrors this exact string by construction.
        ServiceChangeOrderEconomics.ContextRef(7, 8, 9).Should().Be("SR-7-OFFER-8-CO-9");
    }
}
