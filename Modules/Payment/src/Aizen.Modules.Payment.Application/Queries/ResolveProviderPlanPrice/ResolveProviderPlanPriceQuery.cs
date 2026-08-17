using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Application.Queries.ResolveProviderPlanPrice;

/// <summary>Admin/dev: resolves the single active plan price at an instant (defaults to now).</summary>
public sealed class ResolveProviderPlanPriceQuery : AizenQuery<ProviderPlanPriceResult>
{
    public required long          ProviderPlanId { get; init; }
    public string                 CurrencyCode   { get; init; } = "TRY";
    public BillingPeriod          BillingPeriod  { get; init; } = BillingPeriod.Monthly;
    public DateTime?              AtUtc          { get; init; }
}

public sealed record ProviderPlanPriceResult(
    long                  PriceId,
    long                  ProviderPlanId,
    ProviderPlanPriceType PriceType,
    BillingPeriod         BillingPeriod,
    decimal               PriceAmount,
    string                CurrencyCode,
    DateTime              EffectiveFrom,
    DateTime?             EffectiveTo,
    string?               PriceCode);
