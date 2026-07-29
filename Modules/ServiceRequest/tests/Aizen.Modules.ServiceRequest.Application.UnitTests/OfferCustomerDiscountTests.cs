using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Application.Services;
using Aizen.Modules.ServiceRequest.Domain.Entities.Offer;
using FluentAssertions;

namespace Aizen.Modules.ServiceRequest.Application.UnitTests;

/// <summary>
/// BE-S6 — deterministic customer-discount allocation (§3) + pre-tax application with tax recompute + CommissionBase
/// reduction (§4). The allocator is pure; the calc extension leaves narrow-core output byte-identical when no discount.
/// </summary>
public sealed class OfferCustomerDiscountTests
{
    private static readonly OfferCalculationService Calc = new();

    private static ServiceRequestOfferItemEntity Item(
        ServiceRequestOfferItemType type, decimal qty, decimal unitPrice, decimal taxRate = 0m)
        => ServiceRequestOfferItemEntity.Create(0, type, $"{type}", null, qty, unitPrice, "USD", 0,
            null, taxRate, OfferDiscountType.None, null, PricingMethod.Fixed, null);

    private static ServiceRequestOfferEntity Build(params ServiceRequestOfferItemEntity[] items)
    {
        var offer = ServiceRequestOfferEntity.Create(1, 2, 3, 0m, "USD", null, null, null, null, null, null);
        foreach (var i in items) offer.AddItem(i);
        return offer;
    }

    private static DiscountAllocationLine L(string r, decimal b) => new(r, b);

    // ── Allocator: deterministic pro-rata, Σ == requested, remainder on largest ──

    [Fact]
    public void Allocator_ProRata_SumsToRequested_RemainderOnLargest()
    {
        var lines = new[] { L("a", 333.33m), L("b", 333.33m), L("c", 333.34m) };
        var res = OfferCustomerDiscountAllocator.Allocate(lines, 100m, CustomerDiscountFundingMode.PlatformFunded, 0m, 0m, true);

        res.Sum(r => r.CustomerDiscount).Should().Be(100m);            // Σ == requested exactly
        res.Single(r => r.LineRef == "c").CustomerDiscount.Should().Be(33.34m);   // remainder on the largest-base line
        res.Single(r => r.LineRef == "a").CustomerDiscount.Should().Be(33.33m);
    }

    [Fact]
    public void Allocator_ClampsToEligibleBase()
    {
        var lines = new[] { L("a", 600m), L("b", 400m) };
        var res = OfferCustomerDiscountAllocator.Allocate(lines, 5000m, CustomerDiscountFundingMode.PlatformFunded, 0m, 0m, true);
        res.Sum(r => r.CustomerDiscount).Should().Be(1000m);          // clamped to Σ base
    }

    // ── Allocator: funding modes + consent ──────────────────────────────────────

    [Fact]
    public void Allocator_Platform_AllToPlatform()
    {
        var res = OfferCustomerDiscountAllocator.Allocate(new[] { L("a", 1000m) }, 100m, CustomerDiscountFundingMode.PlatformFunded, 0m, 0m, true);
        res[0].PlatformFunded.Should().Be(100m);
        res[0].ProviderFunded.Should().Be(0m);
        res[0].CustomerDiscount.Should().Be(100m);
    }

    [Theory]
    [InlineData(true, 100, 100)]    // consent → provider funds, discount applies
    [InlineData(false, 0, 0)]       // no consent → provider portion dropped, NOT platform-shifted → no discount
    public void Allocator_Provider_ConsentHonoured(bool consent, decimal expectedProvider, decimal expectedApplied)
    {
        var res = OfferCustomerDiscountAllocator.Allocate(new[] { L("a", 1000m) }, 100m, CustomerDiscountFundingMode.ProviderFunded, 0m, 1m, consent);
        res[0].ProviderFunded.Should().Be(expectedProvider);
        res[0].PlatformFunded.Should().Be(0m);                        // never platform-shifted
        res[0].CustomerDiscount.Should().Be(expectedApplied);
    }

    [Theory]
    [InlineData(true, 60, 40, 100)]   // shared 60/40, consent → both apply
    [InlineData(false, 60, 0, 60)]    // no consent → only the platform 60% applies (provider 40% dropped)
    public void Allocator_Shared_SplitByRates_ConsentDropsProviderPortion(
        bool consent, decimal expPlatform, decimal expProvider, decimal expApplied)
    {
        var res = OfferCustomerDiscountAllocator.Allocate(new[] { L("a", 1000m) }, 100m, CustomerDiscountFundingMode.Shared, 0.6m, 0.4m, consent);
        res[0].PlatformFunded.Should().Be(expPlatform);
        res[0].ProviderFunded.Should().Be(expProvider);
        res[0].CustomerDiscount.Should().Be(expApplied);
    }

    // ── Smoke (spec §9.4): Service 5000 eligible + Travel 800 exempt, GOLD 5% PlatformFunded ──

    [Fact]
    public void Calc_Smoke_ServiceDiscounted_TravelUntouched_CommissionBaseReduced()
    {
        var offer = Build(
            Item(ServiceRequestOfferItemType.Service, 1, 5000m, taxRate: 0.20m),   // Eligible discount + Eligible commission
            Item(ServiceRequestOfferItemType.Travel,  1, 800m,  taxRate: 0.20m));  // Exempt discount + Exempt commission

        // GOLD 5% of eligible base (5000) = 250, PlatformFunded.
        Calc.Calculate(offer, new CustomerDiscountSpec(
            RequestedAmount: 250m, FundingMode: CustomerDiscountFundingMode.PlatformFunded,
            PlatformRate: 1m, ProviderRate: 0m, ProviderConsent: true, RuleCode: "GOLD"));

        var svc = offer.Items.Single(i => i.ItemType == ServiceRequestOfferItemType.Service);
        var trv = offer.Items.Single(i => i.ItemType == ServiceRequestOfferItemType.Travel);

        // Service: discount 250 pre-tax → tax recomputed on 4750
        svc.CustomerDiscountAmount.Should().Be(250m);
        svc.PlatformFundedDiscountAmount.Should().Be(250m);
        svc.ProviderFundedDiscountAmount.Should().Be(0m);
        svc.TaxAmount.Should().Be(950m);                 // 4750 × 0.20 (recomputed on the discounted base)
        svc.LineTotal.Should().Be(5700m);                // 4750 + 950
        svc.CommissionBaseAmount.Should().Be(4750m);     // commission follows the discounted service value

        // Travel: untouched
        trv.CustomerDiscountAmount.Should().Be(0m);
        trv.TaxAmount.Should().Be(160m);                 // 800 × 0.20
        trv.CommissionBaseAmount.Should().Be(0m);        // commission-exempt

        // Offer aggregates
        offer.TotalCustomerDiscount.Should().Be(250m);
        offer.TotalPlatformFundedDiscount.Should().Be(250m);
        offer.TotalProviderFundedDiscount.Should().Be(0m);
        offer.CommissionBaseTotal.Should().Be(4750m);
    }

    // ── Narrow-core: no customer discount → byte-identical to pre-S6 ─────────────

    [Fact]
    public void Calc_NarrowCore_NoCustomerDiscount_Unchanged()
    {
        var offer = Build(
            Item(ServiceRequestOfferItemType.Service, 1, 5000m, taxRate: 0.20m),
            Item(ServiceRequestOfferItemType.Travel,  1, 800m,  taxRate: 0.20m));

        Calc.Calculate(offer);   // no customer discount

        var svc = offer.Items.Single(i => i.ItemType == ServiceRequestOfferItemType.Service);
        svc.CustomerDiscountAmount.Should().Be(0m);
        svc.TaxAmount.Should().Be(1000m);                // 5000 × 0.20 (full base)
        svc.CommissionBaseAmount.Should().Be(5000m);     // full base
        offer.TotalCustomerDiscount.Should().Be(0m);
        offer.CommissionBaseTotal.Should().Be(5000m);
    }

    // ── Eligibility: exempt line receives 0; eligible lines share it ────────────

    [Fact]
    public void Calc_ExemptLine_GetsNoDiscount_EligibleLinesShare()
    {
        var offer = Build(
            Item(ServiceRequestOfferItemType.Service, 1, 3000m),   // eligible
            Item(ServiceRequestOfferItemType.Labor,   1, 2000m),   // eligible
            Item(ServiceRequestOfferItemType.Travel,  1, 1000m));  // exempt

        // 10% of eligible base (5000) = 500.
        Calc.Calculate(offer, new CustomerDiscountSpec(500m, CustomerDiscountFundingMode.PlatformFunded, 1m, 0m, true, "P10"));

        offer.Items.Single(i => i.ItemType == ServiceRequestOfferItemType.Service).CustomerDiscountAmount.Should().Be(300m);  // 500×3000/5000
        offer.Items.Single(i => i.ItemType == ServiceRequestOfferItemType.Labor).CustomerDiscountAmount.Should().Be(200m);    // 500×2000/5000
        offer.Items.Single(i => i.ItemType == ServiceRequestOfferItemType.Travel).CustomerDiscountAmount.Should().Be(0m);     // exempt
        offer.TotalCustomerDiscount.Should().Be(500m);
    }
}
