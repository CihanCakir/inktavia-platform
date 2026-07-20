using Aizen.Core.CQRS.Handler;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDryProviderCommissionTrend;

public sealed class GetCargoDryProviderCommissionTrendQueryHandler
    : AizenQueryHandler<GetCargoDryProviderCommissionTrendQuery, List<CargoDryEarningsTrendPointDto>>
{
    private readonly ICargoDrySalesAttributionRepository _attributions;
    private readonly ICargoDryProductRepository _products;

    public GetCargoDryProviderCommissionTrendQueryHandler(
        ICargoDrySalesAttributionRepository attributions,
        ICargoDryProductRepository products)
    {
        _attributions = attributions;
        _products     = products;
    }

    public override async Task<List<CargoDryEarningsTrendPointDto>?> Handle(
        GetCargoDryProviderCommissionTrendQuery request, CancellationToken ct)
    {
        var months = Math.Clamp(request.Months, 1, 24);
        var now = DateTimeOffset.UtcNow;
        var currentMonthStart = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);

        var allProducts = await _products.GetAllActiveAsync(ct);
        var currency = allProducts.FirstOrDefault()?.CurrencyCode ?? "USD";

        var points = new List<CargoDryEarningsTrendPointDto>(months);

        for (var i = months - 1; i >= 0; i--)
        {
            var monthStart = currentMonthStart.AddMonths(-i);
            var monthEnd   = monthStart.AddMonths(1);
            var commission = await _attributions.SumProviderCommissionAsync(
                request.ProviderProfileId, monthStart, monthEnd, ct);

            points.Add(new CargoDryEarningsTrendPointDto
            {
                Month        = monthStart.ToString("yyyy-MM"),
                Commission   = commission,
                CurrencyCode = currency,
            });
        }

        return points;
    }
}
