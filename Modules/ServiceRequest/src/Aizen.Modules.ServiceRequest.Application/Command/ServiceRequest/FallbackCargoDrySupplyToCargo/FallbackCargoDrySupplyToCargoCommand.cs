using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Constants;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Domain.Entities.ServiceRequest;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.ServiceRequest.Application.Command.ServiceRequest.FallbackCargoDrySupplyToCargo;

/// <summary>
/// CargoDry supply v2 — no program provider accepted within the window: move the order OUT of the provider pool
/// permanently → AwaitingShipment (direct cargo sale). In-process, system-invoked by the accept-timeout sweep.
/// Deterministic winner vs a concurrent provider accept: guarded to status Open — first writer wins; if a provider
/// already accepted (status != Open) this is a no-op.
/// </summary>
public sealed class FallbackCargoDrySupplyToCargoCommand : AizenCommand<bool>
{
    public long ServiceRequestId { get; init; }
}

public sealed class FallbackCargoDrySupplyToCargoCommandHandler
    : AizenCommandHandler<FallbackCargoDrySupplyToCargoCommand, bool>
{
    private readonly IServiceRequestRepository _repository;
    private readonly ILogger<FallbackCargoDrySupplyToCargoCommandHandler> _logger;

    public FallbackCargoDrySupplyToCargoCommandHandler(
        IServiceRequestRepository repository,
        ILogger<FallbackCargoDrySupplyToCargoCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public override async Task<bool> Handle(FallbackCargoDrySupplyToCargoCommand request, CancellationToken cancellationToken)
    {
        var sr = await _repository.GetByIdAsync(request.ServiceRequestId, cancellationToken);
        if (sr is null || sr.CargoDryProductCode is null ||
            !string.Equals(sr.ServiceCategoryCode, ServiceRequestServiceCategoryCodes.CargoDrySupply, StringComparison.OrdinalIgnoreCase))
            return false;

        // Race guard: only an order still open (not yet accepted) falls back. A provider accept that already flipped it
        // to Assigned wins → no-op.
        if (sr.Status is not (ServiceRequestStatus.Open or ServiceRequestStatus.WaitingForOffer or ServiceRequestStatus.OfferReceived))
        {
            _logger.LogInformation("CargoDry order {SrId} no longer Open (status {Status}); fallback skipped.", sr.Id, sr.Status);
            return false;
        }

        var prev = sr.Status;
        sr.MoveToAwaitingShipment();
        sr.AddStatusHistory(ServiceRequestStatusHistoryEntity.Create(
            sr.Id, prev, ServiceRequestStatus.AwaitingShipment,
            "No program provider accepted within the window → direct cargo sale", sr.OwnerUserId, ServiceRequestActorType.System));
        _repository.Update(sr);

        _logger.LogInformation("CargoDry order {SrId} fell back to cargo (AwaitingShipment).", sr.Id);
        return true;
    }
}
