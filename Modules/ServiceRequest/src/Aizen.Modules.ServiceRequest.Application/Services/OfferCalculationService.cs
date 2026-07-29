using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Domain.Entities.Offer;

namespace Aizen.Modules.ServiceRequest.Application.Services;

/// <summary>
/// BE-S6 — the resolved customer (platform/plan) discount to apply to an offer (§20.10/§19.6). <see cref="RequestedAmount"/>
/// is pre-computed on the eligible base (by P6 via the preview, respecting Min/Max clamps); the allocator distributes it.
/// </summary>
public sealed record CustomerDiscountSpec(
    decimal                     RequestedAmount,
    CustomerDiscountFundingMode FundingMode,
    decimal                     PlatformRate,
    decimal                     ProviderRate,
    bool                        ProviderConsent,
    string?                     RuleCode = null);

/// <summary>
/// Server-authoritative offer calculation. Called by save/preview/submit — the client's numbers are never trusted.
///
/// Algorithm (doc 06):
///   round(x) = Math.Round(x, 2, MidpointRounding.AwayFromZero)
///   For each non-discount line: lineSubtotal = round(quantity * unitPrice); subtotal = Σ lineSubtotal
///   provider discount lines resolve against subtotal → pro-rata per line → postProviderBase = Max(LineSubtotal − proRata, 0)
///   BE-S6: an optional customer discount is allocated over the eligible postProviderBase (pre-tax), subtracted before tax
///   per taxable line: effectiveBase = postProviderBase − customerDiscount; lineTax = round(effectiveBase * taxRate)
///   grandTotal = taxableBase − Σ customerDiscount + taxTotal
///
/// Provider offer discount (ItemType=Discount) and customer discount (BE-S6) are DISTINCT: the former is the provider's own
/// price cut; the latter is a platform/plan discount with a funding source. Both apply pre-tax. When no customer discount is
/// supplied (narrow core), the output is byte-identical to pre-S6.
/// </summary>
public sealed class OfferCalculationService
{
    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    public void Calculate(ServiceRequestOfferEntity offer, CustomerDiscountSpec? customerDiscount = null)
    {
        var items = offer.Items.ToList();
        var pricedLines = items.Where(i => i.ItemType != ServiceRequestOfferItemType.Discount).ToList();
        var discountLines = items.Where(i => i.ItemType == ServiceRequestOfferItemType.Discount).ToList();

        // Step 1: line subtotals for priced lines
        foreach (var line in pricedLines)
        {
            var lineSubtotal = Round(line.Quantity * line.UnitPrice);
            line.SetComputedTotals(lineSubtotal, 0, 0, lineSubtotal);
        }

        var subtotal = pricedLines.Sum(l => l.LineSubtotal);

        // Step 2: resolve provider offer-discount lines
        decimal totalDiscountAmount = 0;
        foreach (var disc in discountLines)
        {
            decimal discAmt = 0;
            if (disc.DiscountType == OfferDiscountType.Amount && disc.DiscountValue.HasValue)
                discAmt = Math.Min(disc.DiscountValue.Value, subtotal);
            else if (disc.DiscountType == OfferDiscountType.Percent && disc.DiscountValue.HasValue)
                discAmt = Round(subtotal * disc.DiscountValue.Value / 100m);

            discAmt = Math.Max(discAmt, 0);
            totalDiscountAmount += discAmt;
            disc.SetComputedTotals(0, discAmt, 0, -discAmt);
        }

        totalDiscountAmount = Math.Min(totalDiscountAmount, subtotal);
        var taxableBase = Math.Max(subtotal - totalDiscountAmount, 0);

        // Step 3a: provider pro-rata per line → post-provider-discount pre-tax base
        var providerLineDiscount = new Dictionary<ServiceRequestOfferItemEntity, decimal>();
        var postProviderBase     = new Dictionary<ServiceRequestOfferItemEntity, decimal>();
        foreach (var line in pricedLines)
        {
            decimal lineDiscount = 0;
            if (subtotal > 0 && totalDiscountAmount > 0)
                lineDiscount = Round(totalDiscountAmount * line.LineSubtotal / subtotal);
            providerLineDiscount[line] = lineDiscount;
            postProviderBase[line]     = Math.Max(line.LineSubtotal - lineDiscount, 0);
        }

        // Step 3b: BE-S6 customer-discount allocation over the eligible post-provider-discount base (pre-tax).
        // Mapped POSITIONALLY (in-memory items share Id/SortOrder, so object identity + list order is the stable key).
        var customerLineDiscount = pricedLines.ToDictionary(l => l, _ => 0m);
        var platformFunded       = pricedLines.ToDictionary(l => l, _ => 0m);
        var providerFunded       = pricedLines.ToDictionary(l => l, _ => 0m);
        if (customerDiscount is not null && customerDiscount.RequestedAmount > 0m)
        {
            var eligibleLines = pricedLines
                .Where(l => l.LineDiscountEligibility != LineDiscountEligibility.Exempt)
                .ToList();
            var eligibleInput = eligibleLines
                .Select((l, idx) => new DiscountAllocationLine(idx.ToString(), postProviderBase[l]))
                .ToList();

            var alloc = OfferCustomerDiscountAllocator.Allocate(
                eligibleInput, customerDiscount.RequestedAmount, customerDiscount.FundingMode,
                customerDiscount.PlatformRate, customerDiscount.ProviderRate, customerDiscount.ProviderConsent);

            for (int k = 0; k < eligibleLines.Count; k++)
            {
                var line = eligibleLines[k];
                customerLineDiscount[line] = alloc[k].CustomerDiscount;
                platformFunded[line]       = alloc[k].PlatformFunded;
                providerFunded[line]       = alloc[k].ProviderFunded;
            }
        }

        // Step 3c: tax recompute per line on the post-customer-discount base
        decimal taxTotal = 0, totalCustomerDiscount = 0, totalPlatform = 0, totalProvider = 0;
        foreach (var line in pricedLines)
        {
            var custD         = customerLineDiscount[line];
            var effectiveBase = Math.Max(postProviderBase[line] - custD, 0);
            var lineTax       = Round(effectiveBase * line.TaxRate);
            var lineTotal     = effectiveBase + lineTax;
            taxTotal += lineTax;

            line.SetComputedTotals(line.LineSubtotal, providerLineDiscount[line], lineTax, lineTotal);
            line.SetCustomerDiscount(custD, platformFunded[line], providerFunded[line]);

            totalCustomerDiscount += custD;
            totalPlatform         += platformFunded[line];
            totalProvider         += providerFunded[line];
        }

        // Customer-facing grand total is reduced by the customer discount (and its VAT, via the lower recomputed taxTotal).
        var grandTotal = Math.Max(taxableBase - totalCustomerDiscount + taxTotal, 0);

        // Step 4: per-category totals (sum of line subtotals by ItemType) — unchanged (pre-discount subtotals).
        decimal Cat(ServiceRequestOfferItemType t) => pricedLines.Where(l => l.ItemType == t).Sum(l => l.LineSubtotal);
        var explicitlyCategorized = new HashSet<ServiceRequestOfferItemType>
        {
            ServiceRequestOfferItemType.Service, ServiceRequestOfferItemType.Product, ServiceRequestOfferItemType.Labor,
            ServiceRequestOfferItemType.Installation, ServiceRequestOfferItemType.Inspection,
            ServiceRequestOfferItemType.Delivery, ServiceRequestOfferItemType.EmergencyFee,
        };
        var otherTotal = pricedLines.Where(l => !explicitlyCategorized.Contains(l.ItemType)).Sum(l => l.LineSubtotal);

        offer.SetComputedTotals(
            subtotal: subtotal,
            discountTotal: totalDiscountAmount,
            taxTotal: taxTotal,
            grandTotal: grandTotal,
            serviceTotal: Cat(ServiceRequestOfferItemType.Service),
            productTotal: Cat(ServiceRequestOfferItemType.Product),
            laborTotal: Cat(ServiceRequestOfferItemType.Labor),
            installationTotal: Cat(ServiceRequestOfferItemType.Installation),
            inspectionTotal: Cat(ServiceRequestOfferItemType.Inspection),
            deliveryTotal: Cat(ServiceRequestOfferItemType.Delivery),
            emergencyFeeTotal: Cat(ServiceRequestOfferItemType.EmergencyFee),
            otherTotal: otherTotal);

        offer.SetCustomerDiscountTotals(totalCustomerDiscount, totalPlatform, totalProvider);

        // Step 5: line-economics pass (BE-S1 + BE-S6) — commission base = post-provider-discount AND post-customer-discount
        // pre-tax revenue share for commission-eligible lines, 0 for commission-Exempt. Commission follows the discounted value.
        decimal commissionBaseTotal = 0m;
        foreach (var line in pricedLines)
        {
            var commissionBase = line.CommissionEligibility == Abstraction.Enum.LineCommissionEligibility.Exempt
                ? 0m
                : Math.Max(line.LineSubtotal - line.DiscountAmount - line.CustomerDiscountAmount, 0m);
            line.SetComputedEconomics(commissionBase);
            commissionBaseTotal += commissionBase;
        }

        commissionBaseTotal = Math.Min(commissionBaseTotal, subtotal);
        offer.SetCommissionBaseTotal(commissionBaseTotal);
    }
}
