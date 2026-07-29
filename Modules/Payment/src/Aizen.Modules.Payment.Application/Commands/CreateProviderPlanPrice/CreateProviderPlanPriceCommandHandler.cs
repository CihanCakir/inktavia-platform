using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.Plan;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Application.Commands.CreateProviderPlanPrice;

[DocumentationInfo("CreateProviderPlanPriceCommandHandler",
    "Admin creates a plan price version. Generates a unique PriceCode, validates the entity, and runs the §4 " +
    "overlap+gap guard (ProviderPlanPriceConflict / ProviderPlanPriceGap) before persisting.")]
public sealed class CreateProviderPlanPriceCommandHandler
    : AizenCommandHandler<CreateProviderPlanPriceCommand, CreateProviderPlanPriceResult>
{
    private readonly IProviderPlanPriceRepository _prices;
    private readonly ILogger<CreateProviderPlanPriceCommandHandler> _logger;

    public CreateProviderPlanPriceCommandHandler(
        IProviderPlanPriceRepository prices,
        ILogger<CreateProviderPlanPriceCommandHandler> logger)
    {
        _prices = prices;
        _logger = logger;
    }

    public override async Task<CreateProviderPlanPriceResult?> Handle(
        CreateProviderPlanPriceCommand request, CancellationToken ct)
    {
        var priceCode = await _prices.GenerateCodeAsync(ct);

        var price = ProviderPlanPriceEntity.Create(
            request.ProviderPlanId,
            request.PriceType,
            request.BillingPeriod,
            request.PriceAmount,
            request.CurrencyCode,
            request.EffectiveFrom.ToUniversalTime(),
            request.EffectiveTo?.ToUniversalTime(),
            priceCode,
            request.Notes);

        GuardOrThrow(await _prices.ValidateInsertableAsync(price, ct));

        await _prices.AddAsync(price, ct);
        // SaveChanges handled by AizenCommandHandlerDecorator.

        _logger.LogInformation(
            "Provider plan price created. Code={Code} PlanId={PlanId} Amount={Amount} [{From} - {To})",
            priceCode, request.ProviderPlanId, request.PriceAmount, request.EffectiveFrom, request.EffectiveTo);

        return new CreateProviderPlanPriceResult(price.Id, priceCode);
    }

    internal static void GuardOrThrow(PlanPriceGuardResult guard)
    {
        switch (guard.Outcome)
        {
            case PlanPriceGuardOutcome.Overlap:
                throw new AizenBusinessException(
                    (int)PaymentErrorCode.ProviderPlanPriceConflict,
                    $"The price range overlaps an existing active price (Id={guard.ConflictingId}).");
            case PlanPriceGuardOutcome.Gap:
                throw new AizenBusinessException(
                    (int)PaymentErrorCode.ProviderPlanPriceGap,
                    "The price range would leave a gap; consecutive prices must be contiguous " +
                    "(prev.EffectiveTo == next.EffectiveFrom).");
        }
    }
}
