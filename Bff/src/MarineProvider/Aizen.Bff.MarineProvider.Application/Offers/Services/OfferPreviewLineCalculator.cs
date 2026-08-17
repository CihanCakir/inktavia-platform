using Aizen.Bff.MarineProvider.Application.Offers.Contracts;

namespace Aizen.Bff.MarineProvider.Application.Offers.Services;

/// <summary>
/// Mirrors the S1 OfferCalculationService logic for preview purposes:
/// computes per-line CommissionBaseAmount, CommissionEligibility, and LineProviderRevenue
/// from raw FE inputs (numeric itemType, quantity, unitPrice, taxRate, offer-level discount).
/// ItemType is the numeric ServiceRequestOfferItemType enum value the FE sends.
/// </summary>
public static class OfferPreviewLineCalculator
{
    // ServiceRequestOfferItemType enum values
    private const int Service              = 1;
    private const int Product              = 2;
    private const int Installation         = 3;
    private const int Delivery             = 4;
    private const int Labor                = 5;
    private const int Inspection           = 6;
    private const int EmergencyFee         = 7;
    // Discount = 8 — not a priced line, skip
    private const int Consumable           = 9;
    private const int Travel               = 10;
    private const int ExternalService      = 11;
    private const int EquipmentRental      = 12;
    private const int MarinaOrLiftFee      = 13;
    private const int OtherApprovedExpense = 14;
    // Other = 99

    /// <summary>
    /// Item types eligible for commission by default.
    /// Mirrors ServiceRequestOfferItemEntity.DefaultCommissionEligibility.
    /// </summary>
    private static readonly HashSet<int> EligibleItemTypes = [
        Service, Labor, Installation, Inspection, EmergencyFee,
        Delivery, Product, Consumable,
    ];

    /// <summary>Item types exempt from commission by default.</summary>
    private static readonly HashSet<int> ExemptItemTypes = [
        Travel, ExternalService, EquipmentRental, MarinaOrLiftFee, OtherApprovedExpense,
    ];

    /// <summary>Derives LineCommissionEligibility from numeric itemType or explicit string override.</summary>
    public static string DeriveCommissionEligibility(int itemType, string? explicitOverride)
    {
        if (!string.IsNullOrEmpty(explicitOverride))
            return explicitOverride;

        if (EligibleItemTypes.Contains(itemType)) return "Eligible";
        if (ExemptItemTypes.Contains(itemType))   return "Exempt";
        return "InheritFromCategory";
    }

    /// <summary>
    /// Computes per-line economics from raw inputs + offer-level discount pro-rata allocation.
    /// </summary>
    public static List<ComputedLineEconomics> ComputeLines(
        IReadOnlyList<OfferCommissionLineInputBff> lines,
        decimal offerDiscountAmount)
    {
        var results = new List<ComputedLineEconomics>(lines.Count);

        var subtotals = lines.Select(l => Math.Round(l.Quantity * l.UnitPrice, 2)).ToArray();
        var totalSubtotal = subtotals.Sum();

        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            var lineSubtotal = subtotals[i];

            // Pro-rata offer-discount allocation (S1 §3a)
            var proRataDiscount = totalSubtotal > 0
                ? Math.Round(offerDiscountAmount * lineSubtotal / totalSubtotal, 2)
                : 0m;

            var postDiscountBase = Math.Max(lineSubtotal - proRataDiscount, 0m);

            var eligibility = DeriveCommissionEligibility(line.ItemType, line.CommissionEligibility);

            var commissionBase = eligibility.Equals("Exempt", StringComparison.OrdinalIgnoreCase)
                ? 0m
                : postDiscountBase;

            results.Add(new ComputedLineEconomics
            {
                LineRef               = line.LineRef,
                CommissionEligibility = eligibility,
                CommissionBaseAmount  = commissionBase,
                LineProviderRevenue   = postDiscountBase,
                LineType              = MapItemTypeToPaymentLineType(line.ItemType),
                ProductCode           = line.ProductCode,
                CategoryCode          = line.CategoryCode,
            });
        }

        return results;
    }

    /// <summary>Computes EligibleServiceBaseAmount from raw discount-preview lines.</summary>
    public static decimal ComputeEligibleServiceBase(
        IReadOnlyList<OfferDiscountLineInputBff> lines,
        decimal offerDiscountAmount)
    {
        var subtotals = lines.Select(l => Math.Round(l.Quantity * l.UnitPrice, 2)).ToArray();
        var totalSubtotal = subtotals.Sum();
        var eligibleBase = 0m;

        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            var lineSubtotal = subtotals[i];

            var proRataDiscount = totalSubtotal > 0
                ? Math.Round(offerDiscountAmount * lineSubtotal / totalSubtotal, 2)
                : 0m;

            var postDiscountBase = Math.Max(lineSubtotal - proRataDiscount, 0m);

            if (EligibleItemTypes.Contains(line.ItemType))
                eligibleBase += postDiscountBase;
        }

        return eligibleBase;
    }

    private static string? MapItemTypeToPaymentLineType(int itemType) => itemType switch
    {
        Labor or Installation or Inspection or EmergencyFee => "Labor",
        Product or Consumable => "Part",
        Travel => "Travel",
        ExternalService or EquipmentRental or MarinaOrLiftFee or OtherApprovedExpense => "PassThrough",
        _ => null, // Service, Delivery, etc.
    };
}

public sealed class ComputedLineEconomics
{
    public string  LineRef               { get; init; } = default!;
    public string  CommissionEligibility { get; init; } = default!;
    public decimal CommissionBaseAmount  { get; init; }
    public decimal LineProviderRevenue   { get; init; }
    public string? LineType              { get; init; }
    public string? ProductCode           { get; init; }
    public string? CategoryCode          { get; init; }
}
