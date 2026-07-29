using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Application.Commands.CreateProviderPlanPrice;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Application.Commands.UpdateProviderPlanPrice;

[DocumentationInfo("UpdateProviderPlanPriceCommandHandler",
    "Admin updates a plan price version's amount/window. Re-validates and runs the §4 overlap+gap guard " +
    "(self excluded). Throws ProviderPlanPriceNotFound if the price does not exist.")]
public sealed class UpdateProviderPlanPriceCommandHandler
    : AizenCommandHandler<UpdateProviderPlanPriceCommand, UpdateProviderPlanPriceResult>
{
    private readonly IProviderPlanPriceRepository _prices;
    private readonly ILogger<UpdateProviderPlanPriceCommandHandler> _logger;

    public UpdateProviderPlanPriceCommandHandler(
        IProviderPlanPriceRepository prices,
        ILogger<UpdateProviderPlanPriceCommandHandler> logger)
    {
        _prices = prices;
        _logger = logger;
    }

    public override async Task<UpdateProviderPlanPriceResult?> Handle(
        UpdateProviderPlanPriceCommand request, CancellationToken ct)
    {
        var price = await _prices.GetByIdAsync(request.Id, ct)
            ?? throw new AizenBusinessException((int)PaymentErrorCode.ProviderPlanPriceNotFound);

        price.Update(
            request.PriceType,
            request.BillingPeriod,
            request.PriceAmount,
            request.EffectiveFrom.ToUniversalTime(),
            request.EffectiveTo?.ToUniversalTime(),
            request.Notes);

        CreateProviderPlanPriceCommandHandler.GuardOrThrow(await _prices.ValidateInsertableAsync(price, ct));

        _prices.Update(price);
        // SaveChanges handled by AizenCommandHandlerDecorator.

        _logger.LogInformation("Provider plan price updated. Id={Id} Code={Code}", price.Id, price.PriceCode);
        return new UpdateProviderPlanPriceResult(price.Id, price.PriceCode);
    }
}
