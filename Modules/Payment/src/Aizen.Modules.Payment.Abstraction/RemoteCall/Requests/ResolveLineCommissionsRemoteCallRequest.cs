using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Abstraction.RemoteCall.Requests;

/// <summary>
/// Request: ServiceRequest → Payment to resolve per-line commissions for an offer's priced lines (BE-S7).
/// Read-only / idempotent (pure resolution — no state written). Typed DTOs (never <c>object</c>).
/// </summary>
public sealed class ResolveLineCommissionsRemoteCallRequest
{
    /// <summary>Provider identity shared across all lines.</summary>
    public required long   ProviderProfileId { get; init; }
    public long?           ProviderPlanId    { get; init; }
    public required string CurrencyCode      { get; init; }

    public required List<ResolveLineCommissionInputDto> Lines { get; init; } = new();
}

/// <summary>A single priced offer line to resolve (BE-S7 §1).</summary>
public sealed class ResolveLineCommissionInputDto
{
    /// <summary>Offer-item id / correlation key echoed back on the result.</summary>
    public required string                   LineRef               { get; init; }
    public LineType?                         LineType              { get; init; }
    public string?                           ProductCode           { get; init; }
    public required LineCommissionEligibility CommissionEligibility { get; init; }
    public string?                           CategoryCode          { get; init; }
    /// <summary>From S1: pre-tax, post-line-discount commission base (Exempt = 0).</summary>
    public required decimal                  CommissionBaseAmount  { get; init; }
    /// <summary>Provider-receivable for the line = Max(LineSubtotal − proRataDiscount, 0). Pre-tax.</summary>
    public required decimal                  LineProviderRevenue   { get; init; }
}
