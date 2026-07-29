using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Payment.Application.Services;
using Aizen.Modules.Payment.Domain.Entities.PlatformFee;

namespace Aizen.Modules.Payment.Application.Queries.ResolvePlatformFee;

[DocumentationInfo("ResolvePlatformFeeQueryHandler",
    "Resolves the platform fee rule for the given context (currency/category/customerType) and computes the " +
    "net/vat/gross breakdown on the supplied base. Throws PlatformFeeRuleConflict on a fail-loud tie and " +
    "PlatformFeeRuleNotFound when nothing matches.")]
public sealed class ResolvePlatformFeeQueryHandler
    : AizenQueryHandler<ResolvePlatformFeeQuery, PlatformFeeResolveResult>
{
    private readonly PlatformFeeCalculationService _calc;

    public ResolvePlatformFeeQueryHandler(PlatformFeeCalculationService calc) => _calc = calc;

    public override async Task<PlatformFeeResolveResult?> Handle(
        ResolvePlatformFeeQuery request, CancellationToken ct)
    {
        var ctx = new PlatformFeeResolveContext(
            CurrencyCode: request.CurrencyCode,
            CategoryCode: request.CategoryCode,
            CustomerType: request.CustomerType);

        var result = await _calc.CalculateAsync(request.CustomerPayableServiceAmount, ctx, ct);
        var r = result.Resolution;
        var b = result.Breakdown;

        return new PlatformFeeResolveResult(
            RuleId:          r.RuleId,
            RuleCode:        string.IsNullOrEmpty(r.RuleCode) ? null : r.RuleCode,
            Model:           r.Model,
            Rate:            r.Rate,
            MinAmount:       r.MinAmount,
            MaxAmount:       r.MaxAmount,
            FixedAmount:     r.FixedAmount,
            SpecificityRank: r.SpecificityRank,
            Source:          r.Source,
            FeeNet:          b.Net,
            FeeVat:          b.Vat,
            FeeGross:        b.Gross,
            VatRate:         b.VatRate,
            VatSource:       b.VatSource);
    }
}
