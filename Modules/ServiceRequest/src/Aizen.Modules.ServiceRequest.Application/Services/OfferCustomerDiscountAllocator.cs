using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Application.Services;

/// <summary>One discount-eligible line fed to the allocator: its pre-tax, post-provider-discount base.</summary>
public sealed record DiscountAllocationLine(string LineRef, decimal EligibleBase);

/// <summary>Per-line allocation result: the applied customer discount + its funding split (Supplier = 0 in the narrow core).</summary>
public sealed record DiscountAllocationResult(
    string  LineRef,
    decimal CustomerDiscount,
    decimal PlatformFunded,
    decimal ProviderFunded,
    decimal SupplierFunded);

/// <summary>
/// BE-S6 §3 — the deterministic, order-stable customer-discount allocator (pure). Distributes the requested discount
/// pro-rata over the eligible lines' pre-tax post-provider-discount base, fixes the rounding remainder on the largest-base
/// line so <c>Σ raw == requested</c> exactly, then splits each line by the funding mode (§19.6). Provider consent (P6):
/// <b>without consent the provider portion is dropped — never platform-shifted</b>, so the customer's applied discount is
/// only the platform-funded portion. Clamp: requested ≤ Σ eligibleBase.
/// </summary>
public static class OfferCustomerDiscountAllocator
{
    private static decimal Round(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);

    public static IReadOnlyList<DiscountAllocationResult> Allocate(
        IReadOnlyList<DiscountAllocationLine> eligible,
        decimal requestedDiscount,
        CustomerDiscountFundingMode fundingMode,
        decimal platformRate,
        decimal providerRate,
        bool providerConsent)
    {
        var results = new List<DiscountAllocationResult>(eligible.Count);
        var totalBase = eligible.Sum(l => l.EligibleBase);
        if (totalBase <= 0m || requestedDiscount <= 0m)
            return eligible.Select(l => new DiscountAllocationResult(l.LineRef, 0m, 0m, 0m, 0m)).ToList();

        // Clamp to the eligible base — the customer discount can never exceed the discountable amount.
        var requested = Math.Min(Round(requestedDiscount), totalBase);

        // ── Deterministic pro-rata; remainder onto the largest-base line (ties → lowest index) ──
        var raw = new decimal[eligible.Count];
        decimal allocated = 0m;
        int largestIdx = 0;
        for (int i = 0; i < eligible.Count; i++)
        {
            raw[i] = Round(requested * eligible[i].EligibleBase / totalBase);
            allocated += raw[i];
            if (eligible[i].EligibleBase > eligible[largestIdx].EligibleBase) largestIdx = i;
        }
        raw[largestIdx] += requested - allocated;            // fix remainder so Σ raw == requested exactly
        if (raw[largestIdx] < 0m) raw[largestIdx] = 0m;

        // ── Funding split per line (§19.6) ──
        for (int i = 0; i < eligible.Count; i++)
        {
            var lineRaw = raw[i];
            decimal platform = 0m, provider = 0m;

            switch (fundingMode)
            {
                case CustomerDiscountFundingMode.PlatformFunded:
                    platform = lineRaw;
                    break;
                case CustomerDiscountFundingMode.ProviderFunded:
                    provider = providerConsent ? lineRaw : 0m;    // no consent → dropped, not platform-shifted
                    break;
                case CustomerDiscountFundingMode.Shared:
                    platform = Round(lineRaw * platformRate);
                    var providerPortion = lineRaw - platform;
                    provider = providerConsent ? providerPortion : 0m;
                    break;
                case CustomerDiscountFundingMode.SupplierFunded:
                default:
                    break;   // reserved → 0 applied
            }

            var applied = platform + provider;   // the customer's ACTUAL discount (may be < raw if consent dropped provider)
            results.Add(new DiscountAllocationResult(eligible[i].LineRef, applied, platform, provider, 0m));
        }

        return results;
    }
}
