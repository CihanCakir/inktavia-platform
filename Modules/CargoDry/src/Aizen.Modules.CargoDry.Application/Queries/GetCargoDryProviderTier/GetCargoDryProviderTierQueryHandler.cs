using Aizen.Core.CQRS.Handler;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Application.Common;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDryProviderTier;

public sealed class GetCargoDryProviderTierQueryHandler
    : AizenQueryHandler<GetCargoDryProviderTierQuery, CargoDryProviderTierDto>
{
    private readonly ICargoDrySalesAttributionRepository _attributions;
    private readonly ICargoDryProductRepository _products;

    public GetCargoDryProviderTierQueryHandler(
        ICargoDrySalesAttributionRepository attributions,
        ICargoDryProductRepository products)
    {
        _attributions = attributions;
        _products     = products;
    }

    public override async Task<CargoDryProviderTierDto?> Handle(
        GetCargoDryProviderTierQuery request, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var currentMonthStart = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var windowStart = currentMonthStart.AddMonths(-11); // rolling 12 months (this month inclusive)
        var endOfTime = now.AddDays(1);

        var cumulative = await _attributions.SumProviderCommissionAsync(
            request.ProviderProfileId, windowStart, endOfTime, ct);

        var current = CargoDryProviderTierConfig.Resolve(cumulative);
        var next = CargoDryProviderTierConfig.Next(current);

        var remaining = next is null ? 0m : Math.Max(0m, next.LowerInclusive - cumulative);

        decimal progressPct;
        if (next is null)
        {
            progressPct = 100m;
        }
        else
        {
            var bandLower = current.LowerInclusive;
            var bandUpper = next.LowerInclusive;
            progressPct = bandUpper > bandLower
                ? Math.Clamp(Math.Round((cumulative - bandLower) / (bandUpper - bandLower) * 100m, 1), 0m, 100m)
                : 100m;
        }

        var allProducts = await _products.GetAllActiveAsync(ct);
        var currency = allProducts.FirstOrDefault()?.CurrencyCode ?? "TRY";

        return new CargoDryProviderTierDto
        {
            CurrentTierCode      = current.Code,
            CurrentTierLabel     = current.Label,
            CurrentBonusRate     = current.BonusRate,
            CumulativeCommission = cumulative,
            NextTierCode         = next?.Code,
            NextTierLabel        = next?.Label,
            NextTierThreshold    = next?.LowerInclusive,
            NextTierBonusRate    = next?.BonusRate,
            RemainingToNextTier  = remaining,
            ProgressPct          = progressPct,
            CurrencyCode         = currency,
            ComputedAtUtc        = now,
        };
    }
}
