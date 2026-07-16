using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Domain.Entities.Offer;

namespace Aizen.Modules.ServiceRequest.Application.Services;

/// <summary>
/// Server-authoritative offer calculation. Called by save/preview/submit — the client's numbers are never trusted.
///
/// Algorithm (doc 06):
///   round(x) = Math.Round(x, 2, MidpointRounding.AwayFromZero)
///   For each non-discount line: lineSubtotal = round(quantity * unitPrice)
///   subtotal = Σ lineSubtotal
///   discountAmount = resolve discount lines against subtotal (Amount → min(value, subtotal); Percent → round(subtotal * value/100))
///   taxableBase = max(subtotal - discountAmount, 0)
///   For each taxable line: pro-rata share of discount, then lineTax = round(lineShareOfBase * taxRate)
///   grandTotal = taxableBase + taxTotal
///
/// Discount percent is 0–100 (e.g. 10 = 10%). Pro-rata: each line bears discount proportional to its lineSubtotal / subtotal.
/// </summary>
public sealed class OfferCalculationService
{
    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    public void Calculate(ServiceRequestOfferEntity offer)
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

        // Step 2: resolve discount lines
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

        // Step 3: pro-rata discount and tax per line
        decimal taxTotal = 0;
        foreach (var line in pricedLines)
        {
            decimal lineDiscount = 0;
            if (subtotal > 0 && totalDiscountAmount > 0)
                lineDiscount = Round(totalDiscountAmount * line.LineSubtotal / subtotal);

            var lineShareOfBase = Math.Max(line.LineSubtotal - lineDiscount, 0);
            var lineTax = Round(lineShareOfBase * line.TaxRate);
            var lineTotal = lineShareOfBase + lineTax;
            taxTotal += lineTax;

            line.SetComputedTotals(line.LineSubtotal, lineDiscount, lineTax, lineTotal);
        }

        var grandTotal = Math.Max(taxableBase + taxTotal, 0);

        // Step 4: per-category totals (sum of line subtotals by ItemType)
        decimal Cat(ServiceRequestOfferItemType t) => pricedLines.Where(l => l.ItemType == t).Sum(l => l.LineSubtotal);

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
            otherTotal: Cat(ServiceRequestOfferItemType.Other));
    }
}
