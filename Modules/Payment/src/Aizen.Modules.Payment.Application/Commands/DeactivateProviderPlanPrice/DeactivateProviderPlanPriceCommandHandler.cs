using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Application.Commands.DeactivateProviderPlanPrice;

[DocumentationInfo("DeactivateProviderPlanPriceCommandHandler",
    "Admin deactivates a plan price version (Status=Inactive, IsActive=false). " +
    "Throws ProviderPlanPriceNotFound if the price does not exist. " +
    "Note: deactivating may open a coverage gap — the admin should add a replacement range.")]
public sealed class DeactivateProviderPlanPriceCommandHandler
    : AizenCommandHandler<DeactivateProviderPlanPriceCommand, DeactivateProviderPlanPriceResult>
{
    private readonly IProviderPlanPriceRepository _prices;
    private readonly ILogger<DeactivateProviderPlanPriceCommandHandler> _logger;

    public DeactivateProviderPlanPriceCommandHandler(
        IProviderPlanPriceRepository prices,
        ILogger<DeactivateProviderPlanPriceCommandHandler> logger)
    {
        _prices = prices;
        _logger = logger;
    }

    public override async Task<DeactivateProviderPlanPriceResult?> Handle(
        DeactivateProviderPlanPriceCommand request, CancellationToken ct)
    {
        var price = await _prices.GetByIdAsync(request.Id, ct)
            ?? throw new AizenBusinessException((int)PaymentErrorCode.ProviderPlanPriceNotFound);

        price.Deactivate();
        _prices.Update(price);
        // SaveChanges handled by AizenCommandHandlerDecorator.

        _logger.LogInformation("Provider plan price deactivated. Id={Id} Code={Code}", price.Id, price.PriceCode);
        return new DeactivateProviderPlanPriceResult(price.Id, price.PriceCode);
    }
}
