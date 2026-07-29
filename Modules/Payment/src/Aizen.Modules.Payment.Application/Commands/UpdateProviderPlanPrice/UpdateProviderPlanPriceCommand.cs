using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Application.Commands.UpdateProviderPlanPrice;

public sealed class UpdateProviderPlanPriceCommand : AizenCommand<UpdateProviderPlanPriceResult>
{
    public required long         Id            { get; init; }
    public ProviderPlanPriceType PriceType     { get; init; } = ProviderPlanPriceType.List;
    public BillingPeriod         BillingPeriod { get; init; } = BillingPeriod.Monthly;
    public required decimal      PriceAmount   { get; init; }
    public required DateTime     EffectiveFrom { get; init; }
    public DateTime?             EffectiveTo   { get; init; }
    public string?               Notes         { get; init; }
}

public sealed record UpdateProviderPlanPriceResult(long Id, string? PriceCode);
