using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.CargoDry.Abstraction.Message;
using Aizen.Modules.CargoDry.Application.Commands.RecordCargoDryDirectSale;
using Aizen.Modules.CargoDry.Application.Commands.RecordCargoDrySupplySale;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.CargoDry.Consumers;

/// <summary>
/// CargoDry supply v2 — records the retail sale in-process when the ServiceRequest module completes a CARGODRY_SUPPLY
/// order via a job (delivered auto-complete, or cargo auto/manual complete). No token needed (in-process ISender).
/// Provider path → SalesAttribution + commission (RecordCargoDrySupplySale, create-if-missing for the delivered kit);
/// cargo path → CargoDryDirectSale (no provider/commission). Both idempotent per source SR.
/// </summary>
public sealed class CargoDrySupplyOrderCompletedConsumer
    : AizenBaseMessageConsumer<CargoDrySupplyOrderCompletedMessage>
{
    private readonly ISender _sender;
    private readonly ILogger<CargoDrySupplyOrderCompletedConsumer> _logger;

    public CargoDrySupplyOrderCompletedConsumer(IServiceProvider sp) : base(sp)
    {
        _sender = sp.GetRequiredService<ISender>();
        _logger = sp.GetRequiredService<ILogger<CargoDrySupplyOrderCompletedConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(
        CargoDrySupplyOrderCompletedMessage message, CancellationToken ct) => Task.FromResult(true);

    public override async Task ExecuteCommitMessage(
        CargoDrySupplyOrderCompletedMessage message, CancellationToken ct)
    {
        if (message.IsCargoSale)
        {
            await _sender.Send(new RecordCargoDryDirectSaleCommand
            {
                ServiceRequestId = message.ServiceRequestId,
                ProductCode      = message.ProductCode,
                SaleAmount       = message.SaleAmount,
                CurrencyCode     = message.CurrencyCode,
                KitId            = message.DeliveredKitId,
                TrackingCode     = message.TrackingCode,
                ShippedAtUtc     = message.ShippedAtUtc?.UtcDateTime,
            }, ct);
            return;
        }

        // Provider path — needs the delivered kit to accrue the attribution + commission.
        if (message.DeliveredKitId is not { } kitId || kitId <= 0)
        {
            _logger.LogWarning(
                "CargoDry supply order {SrId} completed on the provider path but has no delivered kit — sale not recorded.",
                message.ServiceRequestId);
            return;
        }

        await _sender.Send(new RecordCargoDrySupplySaleCommand
        {
            KitId            = kitId,
            ServiceRequestId = message.ServiceRequestId,
            OwnerUserId      = message.OwnerUserId,
            SaleAmount       = message.SaleAmount,
            CurrencyCode     = message.CurrencyCode,
            ResolvedByUserId = message.ResolvedByUserId,
        }, ct);
    }

    public override Task ExecuteRollbackMessage(
        CargoDrySupplyOrderCompletedMessage message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogError("CargoDrySupplyOrderCompletedConsumer rollback for SR {SrId}: {Error}",
            message.ServiceRequestId, ex.Message);
        return Task.CompletedTask;
    }
}
