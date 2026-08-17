using Aizen.Bff.MarineProvider.Application.Offers.Contracts;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.RemoteCall.Responses;

namespace Aizen.Bff.MarineProvider.Application.Offers;

/// <summary>
/// BE-S7 offer-builder commission preview (compute-on-demand, nothing persisted). Resolves per-line commission
/// (base/rate/amount/providerNet) + transaction totals for the draft lines via the Payment internal resolver. The
/// caller's <c>ProviderProfileId</c> is resolved by-subject (never taken from the request). Optional
/// <see cref="ProviderPlanId"/> so plan-tier rates resolve correctly (else the resolver falls back / defaults).
/// </summary>
public sealed class GetOfferCommissionPreviewBffQuery : AizenQuery<ResolveLineCommissionsRemoteCallResponse>
{
    public long?                            ProviderPlanId      { get; init; }
    public string                           CurrencyCode        { get; init; } = "TRY";
    public decimal                          OfferDiscountAmount { get; init; }
    public List<OfferCommissionLineInputBff> Lines              { get; init; } = new();
}
