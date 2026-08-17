using Aizen.Bff.MarineProvider.Application.Offers.Contracts;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.RemoteCall.Responses;

namespace Aizen.Bff.MarineProvider.Application.Offers;

/// <summary>
/// BE-S6 offer-builder customer-discount preview. The BFF computes EligibleServiceBaseAmount from raw line inputs
/// (server owns totals), then proxies to Payment's customer-discount resolver.
/// </summary>
public sealed class GetOfferCustomerDiscountPreviewBffQuery : AizenQuery<ResolveCustomerDiscountRemoteCallResponse>
{
    public long?   CustomerPlanId      { get; init; }
    public string? CategoryCode        { get; init; }
    public string  CurrencyCode        { get; init; } = "TRY";
    public decimal OfferDiscountAmount { get; init; }
    public List<OfferDiscountLineInputBff> Lines { get; init; } = new();
}
