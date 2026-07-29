using System.Globalization;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Application.Gateway.Iyzico.Models;

namespace Aizen.Modules.Payment.Application.Gateway.Iyzico;

/// <summary>
/// BE-P9 §2 — the pre-send split-math self-verification guard (zero tolerance). Runs against the ALREADY-BUILT basket (the
/// exact figures that would go on the wire) BEFORE any iyzico call. Any mismatch → throws
/// <see cref="PaymentErrorCode.IyzicoSplitMathMismatch"/> (5093) naming the failed relationship — so a mis-split can never
/// reach the gateway even if an upstream (P8/S8/I1) regresses. Pure; unit-tested with synthetic snapshots.
/// </summary>
public static class IyzicoSplitMathGuard
{
    /// <summary>
    /// Asserts (§2): Price==PaidPrice==CustomerTotal; Σ basket.Price == CustomerTotal; when a split is required,
    /// Σ basket.SubMerchantPrice == ProviderNetTotal, every split line carries a SubMerchantKey and SubMerchantPrice ≤ Price;
    /// retained = CustomerTotal − ProviderNetTotal ≥ 0 and == <paramref name="expectedRetained"/> when supplied
    /// (the snapshot's PlatformGrossShare = commission + platform fee (+ service VAT), an independent third value).
    /// </summary>
    public static void Verify(
        decimal customerTotal,
        decimal providerNetTotal,
        IReadOnlyList<IyzicoBasketItem> basket,
        bool requireSplit,
        decimal? expectedRetained = null)
    {
        if (basket is null || basket.Count == 0)
            Fail("basket is empty", customerTotal, providerNetTotal, 0m, 0m);

        decimal basketSum = 0m, splitSum = 0m;
        foreach (var item in basket!)
        {
            var price = Parse(item.Price);
            basketSum += price;

            var sub = string.IsNullOrEmpty(item.SubMerchantPrice) ? (decimal?)null : Parse(item.SubMerchantPrice);
            if (sub is { } s)
            {
                splitSum += s;
                if (s > price)
                    Fail($"line SubMerchantPrice {s} > Price {price}", customerTotal, providerNetTotal, basketSum, splitSum);
                if (string.IsNullOrWhiteSpace(item.SubMerchantKey))
                    Fail("split line missing SubMerchantKey", customerTotal, providerNetTotal, basketSum, splitSum);
            }
        }

        // Σ basket.Price == CustomerTotal (what the customer is charged)
        if (basketSum != customerTotal)
            Fail($"Σ basket.Price {basketSum} != CustomerTotal {customerTotal}", customerTotal, providerNetTotal, basketSum, splitSum);

        if (requireSplit)
        {
            // At least one split line must carry the sub-merchant key (BE-I1 IsSplitEligible).
            if (!basket!.Any(i => !string.IsNullOrWhiteSpace(i.SubMerchantKey)))
                Fail("split required but no SubMerchantKey on any line", customerTotal, providerNetTotal, basketSum, splitSum);

            // Σ subMerchantPrice == ProviderNetTotal (the split to the provider sub-merchant)
            if (splitSum != providerNetTotal)
                Fail($"Σ SubMerchantPrice {splitSum} != ProviderNetTotal {providerNetTotal}", customerTotal, providerNetTotal, basketSum, splitSum);
        }

        // Retained (main merchant) ≥ 0
        var retained = customerTotal - providerNetTotal;
        if (retained < 0m)
            Fail($"retained {retained} < 0 (ProviderNet {providerNetTotal} > CustomerTotal {customerTotal})",
                customerTotal, providerNetTotal, basketSum, splitSum);

        // Retained == the snapshot's independently-stored PlatformGrossShare, when supplied.
        if (expectedRetained is { } exp && retained != exp)
            Fail($"retained {retained} != expected (snapshot PlatformGrossShare) {exp}",
                customerTotal, providerNetTotal, basketSum, splitSum);
    }

    private static decimal Parse(string s) => decimal.Parse(s, NumberStyles.Number, CultureInfo.InvariantCulture);

    private static void Fail(string why, decimal customerTotal, decimal providerNet, decimal basketSum, decimal splitSum)
        => throw new AizenBusinessException((int)PaymentErrorCode.IyzicoSplitMathMismatch,
            $"Split-math guard: {why} [CustomerTotal={customerTotal}, ProviderNet={providerNet}, ΣPrice={basketSum}, ΣSubMerchantPrice={splitSum}].");
}
