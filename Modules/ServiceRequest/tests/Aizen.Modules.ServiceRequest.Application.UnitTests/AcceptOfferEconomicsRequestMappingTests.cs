using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Application.Command.Offer;
using Aizen.Modules.ServiceRequest.Application.Services;
using Aizen.Modules.ServiceRequest.Domain.Entities.Offer;
using Aizen.Modules.ServiceRequest.Domain.Entities.ServiceRequest;
using FluentAssertions;
using PayEnum = Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Application.UnitTests;

/// <summary>
/// BE-P8 — the SR → Payment acceptance-economics request builder. Mirrors the S7 preview projection (Discount lines
/// excluded, LineType/eligibility maps reused) and adds the S8 raw ints + per-line gross/vat.
/// </summary>
public sealed class AcceptOfferEconomicsRequestMappingTests
{
    private static readonly OfferCalculationService Calc = new();

    private static ServiceRequestEntity NewSr() =>
        ServiceRequestEntity.Create(
            "SR-CODE", ownerUserId: 77, vesselId: 5, serviceCategoryCode: "ENGINE",
            serviceTypeCode: null, title: "t", description: null, priority: ServiceRequestPriority.Normal,
            requestedStartDate: null, requestedEndDate: null, locationCountryCode: null, locationCityCode: null,
            locationMarinaName: null, locationLatitude: null, locationLongitude: null, ownerNotes: null, expiresAt: null);

    private static ServiceRequestOfferItemEntity Item(
        ServiceRequestOfferItemType type, decimal qty, decimal unitPrice, decimal taxRate = 0m)
        => ServiceRequestOfferItemEntity.Create(0, type, $"{type}", null, qty, unitPrice, "USD", 0,
            null, taxRate, OfferDiscountType.None, null, PricingMethod.Fixed, null);

    private static ServiceRequestOfferEntity BuildOffer(params ServiceRequestOfferItemEntity[] items)
    {
        var offer = ServiceRequestOfferEntity.Create(1, 2, 3, 0m, "USD", null, null, null, null, null, null);
        foreach (var i in items) offer.AddItem(i);
        Calc.Calculate(offer);
        return offer;
    }

    [Fact]
    public void BuildEconomicsRequest_MapsLines_ExcludesDiscount_UsesRawIntsAndProviderRevenue()
    {
        var sr = NewSr();
        var offer = BuildOffer(
            Item(ServiceRequestOfferItemType.Service, 1, 5000m),   // Eligible
            Item(ServiceRequestOfferItemType.Travel, 1, 800m),     // Exempt
            Item(ServiceRequestOfferItemType.Discount, 1, 100m));  // excluded

        var req = AcceptServiceRequestOfferCommandHandler.BuildEconomicsRequest(sr, offer);

        req.CurrencyCode.Should().Be("USD");
        req.ProviderProfileId.Should().Be(offer.ProviderProfileId);
        req.CustomerProfileId.Should().Be(77);
        req.CategoryCode.Should().Be("ENGINE");
        req.IdempotencyKey.Should().Be($"SR-{sr.Id}-OFFER-{offer.Id}");

        req.Lines.Should().HaveCount(2);   // Discount excluded

        var svc = req.Lines.Single(l => l.ItemType == (int)ServiceRequestOfferItemType.Service);
        svc.CommissionLineType.Should().Be(PayEnum.LineType.Labor);
        svc.CommissionEligibility.Should().Be(PayEnum.LineCommissionEligibility.Eligible);
        svc.PricingMethod.Should().Be((int)PricingMethod.Fixed);
        svc.LineGrossBeforeDiscount.Should().Be(5000m);
        svc.LineProviderRevenue.Should().Be(5000m);
        svc.CommissionBaseAmount.Should().Be(5000m);
        svc.LineVat.Should().Be(0m);

        var travel = req.Lines.Single(l => l.ItemType == (int)ServiceRequestOfferItemType.Travel);
        travel.CommissionEligibility.Should().Be(PayEnum.LineCommissionEligibility.Exempt);
        travel.CommissionBaseAmount.Should().Be(0m);            // exempt → base 0 (S1)
        travel.LineProviderRevenue.Should().Be(800m);          // revenue still counted
    }

    [Fact]
    public void BuildEconomicsRequest_CarriesVatPerLine()
    {
        var sr = NewSr();
        var offer = BuildOffer(Item(ServiceRequestOfferItemType.Service, 1, 1000m, taxRate: 0.20m));

        var req = AcceptServiceRequestOfferCommandHandler.BuildEconomicsRequest(sr, offer);

        var line = req.Lines.Single();
        line.LineGrossBeforeDiscount.Should().Be(1000m);
        line.LineVat.Should().Be(200m);
    }
}
