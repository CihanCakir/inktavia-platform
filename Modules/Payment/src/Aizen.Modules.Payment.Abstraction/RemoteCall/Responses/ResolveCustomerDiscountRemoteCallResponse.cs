using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Abstraction.RemoteCall.Responses;

/// <summary>
/// BE-S6 — the resolved P6 customer-discount rule flattened for the SR offer-builder. <see cref="Found"/> false → no rule
/// matched (no customer discount). <see cref="RequestedDiscountAmount"/> is already computed on the request's eligible base
/// (Percent × base or Fixed, Min/Max-clamped). The SR allocator distributes it per line + splits funding.
/// </summary>
public sealed class ResolveCustomerDiscountRemoteCallResponse
{
    public required bool Found { get; init; }

    public string?                     RuleCode                { get; init; }
    public CustomerDiscountType        DiscountType            { get; init; }
    public decimal                     RequestedDiscountAmount { get; init; }
    public CustomerDiscountFundingMode FundingMode             { get; init; }
    public decimal                     PlatformFundingRate     { get; init; }   // 0 when N/A; Shared → sums to 1.0 with provider
    public decimal                     ProviderFundingRate     { get; init; }
    public bool                        RequiresProviderConsent { get; init; }
}
