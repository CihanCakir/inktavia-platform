using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Queries.ResolveProviderPlanPrice;

[DocumentationInfo("ResolveProviderPlanPriceQueryHandler",
    "Point-in-time resolution of the single active ProviderPlanPrice. Throws ProviderPlanPriceConflict on overlap " +
    "and ProviderPlanPriceNotFound when no price covers the instant.")]
public sealed class ResolveProviderPlanPriceQueryHandler
    : AizenQueryHandler<ResolveProviderPlanPriceQuery, ProviderPlanPriceResult>
{
    private readonly IProviderPlanPriceRepository _prices;

    public ResolveProviderPlanPriceQueryHandler(IProviderPlanPriceRepository prices) => _prices = prices;

    public override async Task<ProviderPlanPriceResult?> Handle(
        ResolveProviderPlanPriceQuery request, CancellationToken ct)
    {
        var atUtc = request.AtUtc ?? DateTime.UtcNow;

        var price = await _prices.ResolveAsync(
            request.ProviderPlanId, request.CurrencyCode, request.BillingPeriod, atUtc, ct)
            ?? throw new AizenBusinessException((int)PaymentErrorCode.ProviderPlanPriceNotFound);

        return new ProviderPlanPriceResult(
            PriceId:        price.Id,
            ProviderPlanId: price.ProviderPlanId,
            PriceType:      price.PriceType,
            BillingPeriod:  price.BillingPeriod,
            PriceAmount:    price.PriceAmount,
            CurrencyCode:   price.CurrencyCode,
            EffectiveFrom:  price.EffectiveFrom,
            EffectiveTo:    price.EffectiveTo,
            PriceCode:      price.PriceCode);
    }
}
