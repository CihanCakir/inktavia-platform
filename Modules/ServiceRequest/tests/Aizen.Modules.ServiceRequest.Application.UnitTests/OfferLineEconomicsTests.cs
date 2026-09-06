using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Application.Services;
using Aizen.Modules.ServiceRequest.Domain.Entities.Offer;
using FluentAssertions;

namespace Aizen.Modules.ServiceRequest.Application.UnitTests;

public sealed class OfferLineEconomicsTests
{
    private static readonly OfferCalculationService Calc = new();

    private static ServiceRequestOfferEntity NewOffer() =>
        ServiceRequestOfferEntity.Create(1, 2, 3, 0m, "USD", null, null, null, null, null, null);

    private static ServiceRequestOfferItemEntity Item(
        ServiceRequestOfferItemType type, decimal qty, decimal unitPrice, decimal taxRate = 0m,
        PricingMethod pricing = PricingMethod.Fixed, LineCommissionEligibility? eligibility = null,
        OfferDiscountType discountType = OfferDiscountType.None, decimal? discountValue = null)
        => ServiceRequestOfferItemEntity.Create(0, type, $"{type}", null, qty, unitPrice, "USD", 0,
            null, taxRate, discountType, discountValue, pricing, eligibility);

    private static ServiceRequestOfferEntity Build(params ServiceRequestOfferItemEntity[] items)
    {
        var offer = NewOffer();
        foreach (var i in items) offer.AddItem(i);
        Calc.Calculate(offer);
        return offer;
    }

    // ── Enum extension: new expense types are PRICED and roll into OtherTotal ────

    [Fact]
    public void New_Expense_Types_Are_Priced_And_Roll_Into_OtherTotal()
    {
        var offer = Build(
            Item(ServiceRequestOfferItemType.Service, 1, 1000m),
            Item(ServiceRequestOfferItemType.Travel, 1, 200m),
            Item(ServiceRequestOfferItemType.Consumable, 2, 50m),      // 100
            Item(ServiceRequestOfferItemType.MarinaOrLiftFee, 1, 300m));

        offer.Subtotal.Should().Be(1600m);                 // all priced lines counted
        offer.ServiceTotal.Should().Be(1000m);
        offer.OtherTotal.Should().Be(600m);                // Travel 200 + Consumable 100 + MarinaFee 300
        offer.ProductTotal.Should().Be(0m);                // existing per-type totals unaffected
    }

    // ── Phase-1 distance pricing: the suggested "Yol bedeli / Travel fee" line ──
    // The create flow injects a regular Travel line with qty = distanceKm, unitPrice = ratePerKm, taxed at the
    // prevailing rate. It flows through the unchanged engine: counted in Subtotal/TaxTotal/GrandTotal, exempt only
    // from the commission base.
    [Fact]
    public void TravelFee_Line_Prices_As_Distance_Times_Rate_With_Normal_Tax()
    {
        var offer = Build(
            Item(ServiceRequestOfferItemType.Service, 1, 5000m, taxRate: 0.20m),
            Item(ServiceRequestOfferItemType.Travel, 459.8m, 15m, taxRate: 0.20m)); // distanceKm × ratePerKm

        var travel = offer.Items.First(i => i.ItemType == ServiceRequestOfferItemType.Travel);
        travel.LineSubtotal.Should().Be(6897.00m);        // 459.8 × 15
        travel.TaxAmount.Should().Be(1379.40m);           // × 20%

        offer.Subtotal.Should().Be(11897.00m);            // 5000 + 6897
        offer.TaxTotal.Should().Be(2379.40m);             // 1000 + 1379.40
        offer.GrandTotal.Should().Be(14276.40m);
        offer.CommissionBaseTotal.Should().Be(5000m);     // Travel exempt from commission base; Service eligible
    }

    // ── PricingMethod is descriptive: money math identical to Fixed ─────────────

    [Fact]
    public void PricingMethod_Does_Not_Change_Money_Math()
    {
        var fixedOffer   = Build(Item(ServiceRequestOfferItemType.Labor, 8, 125m, pricing: PricingMethod.Fixed));
        var perHourOffer = Build(Item(ServiceRequestOfferItemType.Labor, 8, 125m, pricing: PricingMethod.PerHour));

        fixedOffer.Subtotal.Should().Be(1000m);
        perHourOffer.Subtotal.Should().Be(fixedOffer.Subtotal);
        perHourOffer.Items.Single().LineSubtotal.Should().Be(1000m);
        perHourOffer.Items.Single().PricingMethod.Should().Be(PricingMethod.PerHour);   // stored
    }

    // ── Commission base: eligible vs exempt + total + ≤ subtotal ────────────────

    [Fact]
    public void CommissionBase_Eligible_Equals_Net_Exempt_Is_Zero()
    {
        var offer = Build(
            Item(ServiceRequestOfferItemType.Service, 1, 1000m),   // Eligible by default
            Item(ServiceRequestOfferItemType.Travel, 1, 200m));    // Exempt by default

        var service = offer.Items.First(i => i.ItemType == ServiceRequestOfferItemType.Service);
        var travel  = offer.Items.First(i => i.ItemType == ServiceRequestOfferItemType.Travel);

        service.CommissionBaseAmount.Should().Be(1000m);   // no discount → base = subtotal
        travel.CommissionBaseAmount.Should().Be(0m);       // pass-through exempt
        offer.CommissionBaseTotal.Should().Be(1000m);      // Σ = only the service line
        offer.CommissionBaseTotal.Should().BeLessThanOrEqualTo(offer.Subtotal);
    }

    [Fact]
    public void Discount_Reduces_Commission_Base_Pro_Rata_And_Pre_Tax()
    {
        // Service 1000 (eligible) + Product 1000 (inherit→base) + 20% offer discount.
        // Pro-rata discount = 200 each; base per line = 1000 − 200 = 800 (pre-tax). Total 1600 ≤ subtotal 2000.
        var offer = Build(
            Item(ServiceRequestOfferItemType.Service, 1, 1000m, taxRate: 0.20m),
            Item(ServiceRequestOfferItemType.Product, 1, 1000m, taxRate: 0.20m),
            Item(ServiceRequestOfferItemType.Discount, 1, 0m, discountType: OfferDiscountType.Percent, discountValue: 20m));

        offer.Subtotal.Should().Be(2000m);
        offer.DiscountTotal.Should().Be(400m);
        foreach (var line in offer.Items.Where(i => i.ItemType != ServiceRequestOfferItemType.Discount))
            line.CommissionBaseAmount.Should().Be(800m);   // pre-tax post-discount, NOT reduced by tax
        offer.CommissionBaseTotal.Should().Be(1600m);
        offer.CommissionBaseTotal.Should().BeLessThanOrEqualTo(offer.Subtotal);
    }

    // ── Explicit override beats the default map ─────────────────────────────────

    [Fact]
    public void Explicit_Eligibility_Override_Is_Honored()
    {
        // A Product line explicitly marked Exempt → base 0 even though the default is InheritFromCategory.
        var offer = Build(Item(ServiceRequestOfferItemType.Product, 1, 500m, eligibility: LineCommissionEligibility.Exempt));
        offer.Items.Single().CommissionBaseAmount.Should().Be(0m);
        offer.CommissionBaseTotal.Should().Be(0m);
    }

    // ── Default eligibility map by economic role ────────────────────────────────

    [Theory]
    [InlineData(ServiceRequestOfferItemType.Service, LineCommissionEligibility.Eligible)]
    [InlineData(ServiceRequestOfferItemType.Labor, LineCommissionEligibility.Eligible)]
    [InlineData(ServiceRequestOfferItemType.Installation, LineCommissionEligibility.Eligible)]
    [InlineData(ServiceRequestOfferItemType.Product, LineCommissionEligibility.InheritFromCategory)]
    [InlineData(ServiceRequestOfferItemType.Consumable, LineCommissionEligibility.InheritFromCategory)]
    [InlineData(ServiceRequestOfferItemType.Travel, LineCommissionEligibility.Exempt)]
    [InlineData(ServiceRequestOfferItemType.MarinaOrLiftFee, LineCommissionEligibility.Exempt)]
    [InlineData(ServiceRequestOfferItemType.ExternalService, LineCommissionEligibility.Exempt)]
    public void Default_Eligibility_Map_By_Economic_Role(ServiceRequestOfferItemType type, LineCommissionEligibility expected)
        => ServiceRequestOfferItemEntity.DefaultEligibilityForItemType(type).Should().Be(expected);

    // ── Determinism / server-authoritative ──────────────────────────────────────

    [Fact]
    public void Calculate_Is_Deterministic_And_Overwrites_Any_Prior_Economics()
    {
        var offer = Build(
            Item(ServiceRequestOfferItemType.Service, 3, 333.33m, taxRate: 0.10m),
            Item(ServiceRequestOfferItemType.Travel, 1, 120m));

        var baseTotal1 = offer.CommissionBaseTotal;
        var lineBase1  = offer.Items.First(i => i.ItemType == ServiceRequestOfferItemType.Service).CommissionBaseAmount;

        Calc.Calculate(offer);   // recompute — must be identical (authoritative, pure)

        offer.CommissionBaseTotal.Should().Be(baseTotal1);
        offer.Items.First(i => i.ItemType == ServiceRequestOfferItemType.Service).CommissionBaseAmount.Should().Be(lineBase1);
        offer.CommissionBaseTotal.Should().BeLessThanOrEqualTo(offer.Subtotal);
    }

    // ── Smoke (spec §8.5): Service + Travel → base = Service net, Travel 0 ───────

    [Fact]
    public void Smoke_Service_Plus_Travel()
    {
        var offer = Build(
            Item(ServiceRequestOfferItemType.Service, 1, 5000m),
            Item(ServiceRequestOfferItemType.Travel, 1, 750m));
        offer.CommissionBaseTotal.Should().Be(5000m);
        offer.Subtotal.Should().Be(5750m);
    }

    // ── FIX_OFFER_LINE_PRICING_ON_CREATE regression guard ───────────────────────
    // CreateServiceRequestOffer now runs this same Calculate before persisting (it previously skipped it → un-priced
    // lines → the accept-time §19.2 "provider −2.14"). A priced Service line must yield non-zero line + total economics.
    [Fact]
    public void Priced_Offer_Has_NonZero_Line_And_Total_Economics()
    {
        var offer = Build(Item(ServiceRequestOfferItemType.Service, 1, 1000m, taxRate: 0.20m));
        var line = offer.Items.Single();

        line.LineSubtotal.Should().Be(1000m);           // was 0 on the un-priced create path
        line.TaxAmount.Should().Be(200m);
        line.CommissionBaseAmount.Should().Be(1000m);   // was 0 → drove the ₺0-service rejection
        offer.GrandTotal.Should().BeGreaterThan(0m);
        // The create/submit empty-lines guard passes only when a non-Discount priced line exists.
        offer.Items.Count(i => i.ItemType != ServiceRequestOfferItemType.Discount && !i.IsDeleted)
             .Should().BeGreaterThan(0);
    }

    // A Discount-only offer has NO priced lines → CreateServiceRequestOffer / SubmitOffer reject it (SR_OFFER_EMPTY),
    // so the degenerate ₺0 offer can't be persisted in the first place.
    [Fact]
    public void DiscountOnly_Offer_Has_No_Priced_Lines()
    {
        var offer = NewOffer();
        offer.AddItem(Item(ServiceRequestOfferItemType.Discount, 1, 0m,
            discountType: OfferDiscountType.Percent, discountValue: 10m));
        Calc.Calculate(offer);

        offer.Items.Count(i => i.ItemType != ServiceRequestOfferItemType.Discount && !i.IsDeleted).Should().Be(0);
    }
}
