using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Money;

namespace Aizen.Modules.Payment.Domain.Entities.CustomerDiscount;

/// <summary>
/// Split of a requested discount across funding sources (§19.6). <see cref="AppliedDiscountAmount"/> is the sum of the
/// funded parts — it may be LESS than the requested amount when a ProviderFunded portion lacks consent (that portion is
/// dropped, never silently platform-funded). <see cref="UnappliedDueToConsent"/> records what was dropped.
/// </summary>
public sealed record CustomerDiscountFundingAllocation(
    decimal PlatformFundedAmount,
    decimal ProviderFundedAmount,
    decimal SupplierFundedAmount,
    decimal AppliedDiscountAmount,
    decimal UnappliedDueToConsent);

/// <summary>
/// Pure funding allocator (§19.6). Binding: platform↔provider are never auto-shifted; ProviderFunded/Shared provider
/// portions apply only with prior explicit provider consent — without it that portion is omitted (not platform-funded).
/// </summary>
public static class CustomerDiscountFundingCalculator
{
    public static CustomerDiscountFundingAllocation Allocate(
        decimal requestedDiscount, CustomerDiscountRuleEntity rule, bool providerConsent)
    {
        requestedDiscount = MoneyMath.Round(requestedDiscount);
        if (requestedDiscount <= 0m)
            return new(0m, 0m, 0m, 0m, 0m);

        switch (rule.FundingMode)
        {
            case CustomerDiscountFundingMode.PlatformFunded:
                return new(requestedDiscount, 0m, 0m, requestedDiscount, 0m);

            case CustomerDiscountFundingMode.ProviderFunded:
                // Requires consent; without it the whole discount is dropped (never platform-funded).
                return providerConsent
                    ? new(0m, requestedDiscount, 0m, requestedDiscount, 0m)
                    : new(0m, 0m, 0m, 0m, requestedDiscount);

            case CustomerDiscountFundingMode.Shared:
            {
                var platform = MoneyMath.Round(requestedDiscount * (rule.PlatformFundingRate ?? 0m));
                var provider = requestedDiscount - platform;   // derived so the split is exact to the kuruş
                // The provider portion applies only with consent; otherwise ONLY the platform portion applies.
                if (rule.RequiresProviderConsent && !providerConsent)
                    return new(platform, 0m, 0m, platform, provider);
                return new(platform, provider, 0m, platform + provider, 0m);
            }

            case CustomerDiscountFundingMode.SupplierFunded:   // reserved — not applied in P6
            default:
                return new(0m, 0m, 0m, 0m, requestedDiscount);
        }
    }
}
