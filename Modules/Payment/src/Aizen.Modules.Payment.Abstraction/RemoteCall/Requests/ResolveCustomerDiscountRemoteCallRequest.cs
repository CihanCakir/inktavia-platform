namespace Aizen.Modules.Payment.Abstraction.RemoteCall.Requests;

/// <summary>
/// BE-S6 — resolves the P6 CustomerDiscountRule for an offer's customer/category/currency and computes the requested
/// discount on the supplied eligible base. Pure read (compute-on-demand) — no persistence, no budget reserve, no snapshot.
/// </summary>
public sealed class ResolveCustomerDiscountRemoteCallRequest
{
    public long?           CustomerPlanId            { get; init; }   // null = any plan (narrow core has no participant plan)
    public string?         CategoryCode              { get; init; }
    public required string CurrencyCode              { get; init; }
    /// <summary>Σ of the discount-eligible lines' pre-tax post-provider-discount base (for the Percent/Min/Max computation).</summary>
    public required decimal EligibleServiceBaseAmount { get; init; }
}
