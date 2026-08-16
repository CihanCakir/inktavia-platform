using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Payment.Abstraction.Dto;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Queries.GetPublicPricingTerms;

[DocumentationInfo("GetPublicPricingTermsQueryHandler",
    "Projects the Global STANDARD commission default + the Global platform-fee headline into the public, web-safe " +
    "PublicPricingTermsDto. Read-only over the published defaults — touches no economics-calculation path. Never " +
    "surfaces per-provider/plan/category rows, cost-share, tevkifat, profit-protection internals or any snapshot.")]
public sealed class GetPublicPricingTermsQueryHandler
    : AizenQueryHandler<GetPublicPricingTermsQuery, PublicPricingTermsDto>
{
    private const string Currency = "TRY";
    private const string CommissionNote =
        "Standard platform commission; individual rates may vary by plan, category or agreement.";

    private readonly ICommissionRuleRepository _commission;
    private readonly IPlatformFeeRuleRepository _platformFee;

    public GetPublicPricingTermsQueryHandler(
        ICommissionRuleRepository commission, IPlatformFeeRuleRepository platformFee)
    {
        _commission = commission;
        _platformFee = platformFee;
    }

    public override async Task<PublicPricingTermsDto?> Handle(
        GetPublicPricingTermsQuery request, CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        // Global + Standard-priority + effective-now commission rule (excludes Emergency / scheduled / expired).
        var commissionRule = await _commission.GetPublishedStandardGlobalRuleAsync(now, ct);
        // Global platform-fee rule (no category, no customer type) for TRY, effective now.
        var feeRule = await _platformFee.GetGlobalRuleAsync(Currency, now, ct);

        var commission = new PublicCommissionTermsDto(
            Audience: "provider",
            Label: "Standard marketplace commission",
            StandardRatePercent: commissionRule is null ? null : ToPercent(commissionRule.CommissionRate),
            Note: CommissionNote);

        PublicPlatformFeeTermsDto? platformFee = feeRule is null
            ? null
            : new PublicPlatformFeeTermsDto(
                Model: feeRule.Model,
                RatePercent: feeRule.Model is PlatformFeeModel.Percentage or PlatformFeeModel.PercentageWithBounds
                    ? (feeRule.Rate is { } rate ? ToPercent(rate) : null)
                    : null,
                MinAmount: feeRule.Model == PlatformFeeModel.PercentageWithBounds ? feeRule.MinAmount : null,
                MaxAmount: feeRule.Model == PlatformFeeModel.PercentageWithBounds ? feeRule.MaxAmount : null,
                Currency: feeRule.CurrencyCode);

        var effectiveFrom = MaxEffectiveFrom(commissionRule?.EffectiveFrom, feeRule?.EffectiveFrom);

        return new PublicPricingTermsDto(Currency, commission, platformFee, effectiveFrom);
    }

    private static decimal ToPercent(decimal fraction) => Math.Round(fraction * 100m, 2);

    // Max EffectiveFrom of the (up to two) source Global rules, normalized to a UTC offset. Null when neither exists.
    private static DateTimeOffset? MaxEffectiveFrom(DateTime? a, DateTime? b)
    {
        DateTime? max = (a, b) switch
        {
            ({ } x, { } y) => x >= y ? x : y,
            ({ } x, null)  => x,
            (null, { } y)  => y,
            _              => null,
        };
        return max is { } value ? new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc)) : null;
    }
}
