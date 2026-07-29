using Aizen.Core.CQRS.Message;

namespace Aizen.Modules.Payment.Application.Commands.DeactivateProviderPlanPrice;

public sealed class DeactivateProviderPlanPriceCommand : AizenCommand<DeactivateProviderPlanPriceResult>
{
    public required long Id { get; init; }
}

public sealed record DeactivateProviderPlanPriceResult(long Id, string? PriceCode);
