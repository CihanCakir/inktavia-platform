using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Abstraction.Dto;

/// <summary>
/// Public, web-safe published pricing terms (M1). The ONLY commercial figures a customer/provider is quoted:
/// the Global STANDARD commission default and the Global platform-fee headline. Everything internal — cost-share
/// rates, tevkifat/withholding mechanics, profit-protection internals, economics/commission-allocation snapshots,
/// per-provider commission benefits/overrides, category/customer-type override rows, rule Notes/RuleCode/user ids —
/// is NOT modelled here, so it cannot leak. VAT is intentionally omitted (its treatment is not a published headline).
/// Served anonymously by <c>PaymentPublicController</c> and folded into the website pricing page by the BFF.
/// </summary>
public sealed record PublicPricingTermsDto(
    string                      Currency,
    PublicCommissionTermsDto    Commission,
    PublicPlatformFeeTermsDto?  CustomerPlatformFee,
    DateTimeOffset?             EffectiveFrom
);

/// <summary>
/// The published STANDARD commission headline. <see cref="StandardRatePercent"/> is the Global Standard-priority,
/// effective-now rule's rate as a percent (null when none resolves — never fabricated). It is a provider-facing
/// term; the note makes explicit that actual rates vary by plan, category or agreement, so no internal per-provider
/// rate is implied.
/// </summary>
public sealed record PublicCommissionTermsDto(
    string   Audience,             // "provider"
    string   Label,
    decimal? StandardRatePercent,
    string   Note
);

/// <summary>
/// The Global platform-fee headline. <see cref="RatePercent"/> is present for Percentage / PercentageWithBounds;
/// <see cref="MinAmount"/>/<see cref="MaxAmount"/> only for PercentageWithBounds. No internal net/vat/gross snapshot,
/// no category/customer-type scope.
/// </summary>
public sealed record PublicPlatformFeeTermsDto(
    PlatformFeeModel Model,
    decimal?         RatePercent,
    decimal?         MinAmount,
    decimal?         MaxAmount,
    string           Currency
);
