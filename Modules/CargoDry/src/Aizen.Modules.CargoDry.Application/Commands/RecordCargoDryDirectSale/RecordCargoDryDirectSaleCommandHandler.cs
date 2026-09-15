using Aizen.Core.CQRS.Handler;
using Aizen.Modules.CargoDry.Domain.Entities;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.CargoDry.Application.Commands.RecordCargoDryDirectSale;

[DocumentationInfo("Record CargoDry direct sale command handler",
    "Records the retail revenue of a cargo (direct online) CargoDry sale in the CargoDry domain (no provider, no " +
    "commission). Idempotent per source SR. Finance/reporting unions this with CargoDrySalesAttribution.")]
public sealed class RecordCargoDryDirectSaleCommandHandler
    : AizenCommandHandler<RecordCargoDryDirectSaleCommand, RecordCargoDryDirectSaleResponse>
{
    private readonly ICargoDryDirectSaleRepository _directSales;
    private readonly ILogger<RecordCargoDryDirectSaleCommandHandler> _logger;

    public RecordCargoDryDirectSaleCommandHandler(
        ICargoDryDirectSaleRepository directSales,
        ILogger<RecordCargoDryDirectSaleCommandHandler> logger)
    {
        _directSales = directSales;
        _logger = logger;
    }

    public override async Task<RecordCargoDryDirectSaleResponse?> Handle(
        RecordCargoDryDirectSaleCommand request, CancellationToken ct)
    {
        var existing = await _directSales.GetBySourceServiceRequestIdAsync(request.ServiceRequestId, ct);
        if (existing is not null)
        {
            _logger.LogInformation("CargoDry direct sale already recorded for SR {SrId} — no-op.", request.ServiceRequestId);
            return new RecordCargoDryDirectSaleResponse { Recorded = false, DirectSaleId = existing.Id, Note = "Already recorded." };
        }

        var entity = CargoDryDirectSaleEntity.Create(
            sourceServiceRequestId: request.ServiceRequestId,
            productCode:            request.ProductCode,
            saleAmount:             request.SaleAmount,
            currencyCode:           request.CurrencyCode,
            completedAtUtc:         DateTime.UtcNow,
            kitId:                  request.KitId,
            trackingCode:           request.TrackingCode,
            shippedAtUtc:           request.ShippedAtUtc);
        await _directSales.AddAsync(entity, ct);
        await _directSales.SaveChangesAsync(ct);

        _logger.LogInformation(
            "CargoDry direct (cargo) sale recorded. SR={SrId} Product={Product} Sale={Sale} {Currency}",
            request.ServiceRequestId, request.ProductCode, request.SaleAmount, request.CurrencyCode);

        return new RecordCargoDryDirectSaleResponse { Recorded = true, DirectSaleId = entity.Id };
    }
}
