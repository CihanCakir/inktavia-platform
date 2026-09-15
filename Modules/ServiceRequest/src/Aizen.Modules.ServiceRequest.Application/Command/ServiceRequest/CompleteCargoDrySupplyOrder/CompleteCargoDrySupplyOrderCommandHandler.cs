using Aizen.Core.CQRS.Handler;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.CargoDry.Abstraction.Message;
using Aizen.Modules.Payment.Abstraction.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;
using Aizen.Modules.ServiceRequest.Domain.Entities.ServiceRequest;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.ServiceRequest.Application.Command.ServiceRequest.CompleteCargoDrySupplyOrder;

[DocumentationInfo("Complete CargoDry supply order command handler",
    "In-process (no token) completion of a CARGODRY_SUPPLY order → completes/closes the SR + publishes escrow-release " +
    "and CargoDry sale-record messages. Idempotent on terminal states. Provider path (delivered) vs cargo path (shipped).")]
public sealed class CompleteCargoDrySupplyOrderCommandHandler
    : AizenCommandHandler<CompleteCargoDrySupplyOrderCommand, CompleteCargoDrySupplyOrderResponse>
{
    private readonly IServiceRequestRepository _repository;
    private readonly IAizenMessagePublisher _publisher;
    private readonly ILogger<CompleteCargoDrySupplyOrderCommandHandler> _logger;

    public CompleteCargoDrySupplyOrderCommandHandler(
        IServiceRequestRepository repository,
        IAizenMessagePublisher publisher,
        ILogger<CompleteCargoDrySupplyOrderCommandHandler> logger)
    {
        _repository = repository;
        _publisher = publisher;
        _logger = logger;
    }

    public override async Task<CompleteCargoDrySupplyOrderResponse?> Handle(
        CompleteCargoDrySupplyOrderCommand request, CancellationToken cancellationToken)
    {
        var sr = await _repository.GetByIdWithDetailsAsync(request.ServiceRequestId, cancellationToken)
            ?? throw new InvalidOperationException($"ServiceRequest {request.ServiceRequestId} not found.");

        if (sr.CargoDryProductCode is null)
            throw new Aizen.Core.Infrastructure.Exception.AizenBusinessException("This is not a CargoDry supply order.");

        // ── Idempotency: terminal states are a no-op (late QR scan / re-run / overlapping sweeps). ──
        if (sr.Status is ServiceRequestStatus.Completed or ServiceRequestStatus.Closed
                       or ServiceRequestStatus.Cancelled or ServiceRequestStatus.DisputeResolved)
        {
            return new CompleteCargoDrySupplyOrderResponse
            { Completed = false, ServiceRequestId = sr.Id, Note = $"Already {sr.Status}." };
        }

        // ── Determine path. Provider = Assigned + delivered; Cargo = Shipped. ──
        var isProvider = sr.Status == ServiceRequestStatus.Assigned && sr.DeliveredAtUtc is not null;
        var isCargo = sr.Status == ServiceRequestStatus.Shipped;
        if (!isProvider && !isCargo)
        {
            return new CompleteCargoDrySupplyOrderResponse
            { Completed = false, ServiceRequestId = sr.Id, Note = $"Not completable from status {sr.Status}." };
        }

        var prevStatus = sr.Status;
        var now = DateTimeOffset.UtcNow;

        sr.MarkCompleted(now);
        sr.AddStatusHistory(ServiceRequestStatusHistoryEntity.Create(
            sr.Id, prevStatus, ServiceRequestStatus.Completed,
            request.Note ?? "CargoDry order completed", sr.OwnerUserId, ServiceRequestActorType.System));
        sr.ReleasePayment(); // → Closed
        sr.AddStatusHistory(ServiceRequestStatusHistoryEntity.Create(
            sr.Id, ServiceRequestStatus.Completed, ServiceRequestStatus.Closed,
            "CargoDry escrow finalized to platform", sr.OwnerUserId, ServiceRequestActorType.System));
        _repository.Update(sr);

        // ── (a) Payment releases the platform-collected escrow in-process (idempotent consumer). ──
        await _publisher.PublishAsync(new ServiceRequestCompletedMessage
        {
            ServiceRequestId  = sr.Id,
            OfferId           = 0,
            ProviderProfileId = sr.Assignment?.ProviderProfileId ?? 0,
            PayerProfileId    = sr.OwnerUserId,
            CompletedAtUtc    = now.UtcDateTime,
            AdminNote         = request.Note ?? "CargoDry order completed",
        }, cancellationToken);

        // ── (b) CargoDry records the retail sale in-process (provider attribution+commission, or cargo direct-sale). ──
        await _publisher.PublishAsync(new CargoDrySupplyOrderCompletedMessage
        {
            ServiceRequestId  = sr.Id,
            ProductCode       = sr.CargoDryProductCode!,
            SaleAmount        = sr.CargoDryRetailAmount ?? 0m,
            CurrencyCode      = sr.CargoDryRetailCurrency ?? "TRY",
            OwnerUserId       = sr.OwnerUserId,
            ProviderProfileId = isProvider ? sr.Assignment?.ProviderProfileId : null,
            DeliveredKitId    = sr.DeliveredKitId, // provider: delivered kit; cargo: shipped kit (optional)
            IsCargoSale       = isCargo,
            TrackingCode      = sr.TrackingCode,
            ShippedAtUtc      = sr.ShippedAtUtc,
            ResolvedByUserId  = 0, // system actor
        }, cancellationToken);

        _logger.LogInformation(
            "CargoDry supply order {SrId} completed ({Path}). Retail={Retail} {Currency}.",
            sr.Id, isCargo ? "cargo" : "provider", sr.CargoDryRetailAmount, sr.CargoDryRetailCurrency);

        return new CompleteCargoDrySupplyOrderResponse
        { Completed = true, ServiceRequestId = sr.Id, IsCargoSale = isCargo };
    }
}
