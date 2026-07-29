using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Payment.Application.Queries.ResolveProviderPlanPrice;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Queries.GetProviderPlanPrices;

[DocumentationInfo("GetProviderPlanPricesQueryHandler", "Lists all price versions for a provider plan.")]
public sealed class GetProviderPlanPricesQueryHandler
    : AizenQueryHandler<GetProviderPlanPricesQuery, List<ProviderPlanPriceResult>>
{
    private readonly IProviderPlanPriceRepository _prices;

    public GetProviderPlanPricesQueryHandler(IProviderPlanPriceRepository prices) => _prices = prices;

    public override async Task<List<ProviderPlanPriceResult>?> Handle(
        GetProviderPlanPricesQuery request, CancellationToken ct)
    {
        var rows = await _prices.GetByPlanAsync(request.ProviderPlanId, ct);

        return rows.Select(price => new ProviderPlanPriceResult(
            PriceId:        price.Id,
            ProviderPlanId: price.ProviderPlanId,
            PriceType:      price.PriceType,
            BillingPeriod:  price.BillingPeriod,
            PriceAmount:    price.PriceAmount,
            CurrencyCode:   price.CurrencyCode,
            EffectiveFrom:  price.EffectiveFrom,
            EffectiveTo:    price.EffectiveTo,
            PriceCode:      price.PriceCode)).ToList();
    }
}
