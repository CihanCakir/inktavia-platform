using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Application.Commands.CreateProviderPlanPrice;

public sealed class CreateProviderPlanPriceCommand : AizenCommand<CreateProviderPlanPriceResult>
{
    public required long          ProviderPlanId { get; init; }
    public ProviderPlanPriceType  PriceType      { get; init; } = ProviderPlanPriceType.List;
    public BillingPeriod          BillingPeriod  { get; init; } = BillingPeriod.Monthly;
    public required decimal       PriceAmount    { get; init; }
    public string                 CurrencyCode   { get; init; } = "TRY";
    public required DateTime      EffectiveFrom  { get; init; }
    public DateTime?              EffectiveTo    { get; init; }
    public string?                Notes          { get; init; }
}

public sealed record CreateProviderPlanPriceResult(long Id, string PriceCode);
