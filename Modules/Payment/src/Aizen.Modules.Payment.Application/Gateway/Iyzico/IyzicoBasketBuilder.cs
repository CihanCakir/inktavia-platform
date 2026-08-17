using System.Globalization;
using Aizen.Modules.Payment.Application.Gateway.Iyzico.Models;

namespace Aizen.Modules.Payment.Application.Gateway.Iyzico;

/// <summary>One priced offer line for the multi-item basket (from the S8 line snapshots).</summary>
/// <param name="LineRef">Snapshot line ref (basket item id).</param>
/// <param name="Name">Human label.</param>
/// <param name="LineTotal">Customer-facing amount for the line (VAT-inclusive).</param>
/// <param name="ProviderNet">Amount routed to the provider sub-merchant for this line.</param>
public sealed record IyzicoSplitLine(string LineRef, string Name, decimal LineTotal, decimal ProviderNet);

/// <summary>
/// BE-P9 §3 — builds the iyzico basket from the P8/S8 snapshot figures. Amounts are formatted with the same F2 invariant
/// convention as the gateway. The result is handed to <see cref="IyzicoSplitMathGuard.Verify"/> before any iyzico call.
/// </summary>
public static class IyzicoBasketBuilder
{
    public static string FormatAmount(decimal amount) => amount.ToString("F2", CultureInfo.InvariantCulture);

    /// <summary>
    /// MVP single sub-merchant, full-release: one basket item, Price = CustomerTotal, SubMerchantPrice = ProviderNetTotal
    /// (retained = commission + platform fee stays with the main merchant). <paramref name="subMerchantKey"/> null →
    /// non-marketplace (no split).
    /// </summary>
    public static List<IyzicoBasketItem> BuildSingle(
        string basketId, string name, decimal customerTotal, decimal providerNetTotal, string? subMerchantKey)
        => new()
        {
            new IyzicoBasketItem
            {
                Id               = basketId,
                Name             = name,
                Category1        = "Marine Services",
                ItemType         = "VIRTUAL",
                Price            = FormatAmount(customerTotal),
                SubMerchantKey   = subMerchantKey,
                SubMerchantPrice = string.IsNullOrWhiteSpace(subMerchantKey) ? null : FormatAmount(providerNetTotal),
            },
        };

    /// <summary>
    /// Item-level (partial/dispute/change-order extension): one basket item per line (SubMerchantPrice = line ProviderNet)
    /// + a single retained platform-fee item (no SubMerchantPrice). Σ Price == CustomerTotal, Σ SubMerchantPrice ==
    /// ProviderNetTotal. Built now; the MVP gate uses the single-basket full-release path.
    /// </summary>
    public static List<IyzicoBasketItem> BuildMultiItem(
        IReadOnlyList<IyzicoSplitLine> lines, decimal platformFeeGross, string subMerchantKey)
    {
        var items = new List<IyzicoBasketItem>(lines.Count + 1);
        foreach (var l in lines)
            items.Add(new IyzicoBasketItem
            {
                Id               = l.LineRef,
                Name             = l.Name,
                Category1        = "Marine Services",
                ItemType         = "VIRTUAL",
                Price            = FormatAmount(l.LineTotal),
                SubMerchantKey   = subMerchantKey,
                SubMerchantPrice = FormatAmount(l.ProviderNet),
            });

        if (platformFeeGross > 0m)
            items.Add(new IyzicoBasketItem
            {
                Id               = "PLATFORM-FEE",
                Name             = "Platform Fee",
                Category1        = "Platform",
                ItemType         = "VIRTUAL",
                Price            = FormatAmount(platformFeeGross),
                SubMerchantKey   = null,     // retained by the main merchant
                SubMerchantPrice = null,
            });

        return items;
    }
}
