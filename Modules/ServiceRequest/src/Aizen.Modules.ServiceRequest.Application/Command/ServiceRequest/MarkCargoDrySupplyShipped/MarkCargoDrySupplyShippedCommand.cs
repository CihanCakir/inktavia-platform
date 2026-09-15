using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.ServiceRequest.Abstraction.Constants;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Message;
using Aizen.Modules.ServiceRequest.Abstraction.RemoteCall;
using Aizen.Modules.ServiceRequest.Application.Common;
using Aizen.Modules.ServiceRequest.Domain.Entities.ServiceRequest;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.ServiceRequest.Application.Command.ServiceRequest.MarkCargoDrySupplyShipped;

/// <summary>
/// CargoDry supply v2 (cargo path) — admin marks an AwaitingShipment order shipped with a carrier tracking code
/// (optionally the shipped kit) and starts the shipped auto-complete window (ShippedAutoCompleteDays). Carrier API
/// integration is out of scope (manual tracking code only).
/// </summary>
public sealed class MarkCargoDrySupplyShippedCommand : AizenCommand<MarkCargoDrySupplyShippedResponse>
{
    public long    ServiceRequestId { get; init; }
    public string  TrackingCode     { get; init; } = default!;
    public long?   KitId            { get; init; }
}

public sealed class MarkCargoDrySupplyShippedResponse
{
    public long     ServiceRequestId        { get; init; }
    public string   TrackingCode            { get; init; } = default!;
    public DateTime ShippedAtUtc            { get; init; }
    public DateTime AutoCompleteDeadlineUtc { get; init; }
}

public sealed class MarkCargoDrySupplyShippedCommandHandler
    : AizenCommandHandler<MarkCargoDrySupplyShippedCommand, MarkCargoDrySupplyShippedResponse>
{
    private readonly IServiceRequestRepository _repository;
    private readonly IServiceRequestReferenceDataRemoteCall _referenceData;
    private readonly IAizenInfoAccessor _info;
    private readonly IAizenMessagePublisher _messagePublisher;
    private readonly ILogger<MarkCargoDrySupplyShippedCommandHandler> _logger;

    public MarkCargoDrySupplyShippedCommandHandler(
        IServiceRequestRepository repository,
        IServiceRequestReferenceDataRemoteCall referenceData,
        IAizenInfoAccessor info,
        IAizenMessagePublisher messagePublisher,
        ILogger<MarkCargoDrySupplyShippedCommandHandler> logger)
    {
        _repository = repository;
        _referenceData = referenceData;
        _info = info;
        _messagePublisher = messagePublisher;
        _logger = logger;
    }

    public override async Task<MarkCargoDrySupplyShippedResponse?> Handle(
        MarkCargoDrySupplyShippedCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.TrackingCode))
            throw new AizenBusinessException("A tracking code is required to mark a cargo order shipped.");

        var sr = await _repository.GetByIdAsync(request.ServiceRequestId, cancellationToken)
            ?? throw new InvalidOperationException($"ServiceRequest {request.ServiceRequestId} not found.");

        if (sr.CargoDryProductCode is null ||
            !string.Equals(sr.ServiceCategoryCode, ServiceRequestServiceCategoryCodes.CargoDrySupply, StringComparison.OrdinalIgnoreCase))
            throw new AizenBusinessException("This is not a CargoDry supply order.");
        if (sr.Status != ServiceRequestStatus.AwaitingShipment)
            throw new AizenBusinessException("Only an AwaitingShipment cargo order can be marked shipped.");

        var days = await CargoDrySupplyOptions.GetShippedAutoCompleteDaysAsync(_referenceData);
        var now = DateTime.UtcNow;
        var deadline = now.AddDays(days);

        sr.MarkCargoShipped(request.TrackingCode.Trim(), now, deadline, request.KitId);
        sr.AddStatusHistory(ServiceRequestStatusHistoryEntity.Create(
            sr.Id, ServiceRequestStatus.AwaitingShipment, ServiceRequestStatus.Shipped,
            $"Cargo shipped (tracking {request.TrackingCode.Trim()}); auto-completes at {deadline:o}",
            _info.UserInfoAccessor.UserInfo.UserId, ServiceRequestActorType.Admin));
        _repository.Update(sr);

        // Wave 4A — owner "kargolandı" (tracking code) + admin feed.
        await _messagePublisher.PublishAsync(new CargoDrySupplyOrderLifecycleMessage
        {
            ServiceRequestId = sr.Id,
            RequestCode      = sr.RequestCode,
            OwnerUserId      = sr.OwnerUserId,
            ProductCode      = sr.CargoDryProductCode,
            Event            = CargoDrySupplyOrderLifecycleEvent.Shipped,
            TrackingCode     = request.TrackingCode.Trim(),
        }, cancellationToken);

        _logger.LogInformation("CargoDry cargo order {SrId} marked shipped; auto-complete deadline {Deadline:o}.", sr.Id, deadline);

        return new MarkCargoDrySupplyShippedResponse
        {
            ServiceRequestId = sr.Id,
            TrackingCode = request.TrackingCode.Trim(),
            ShippedAtUtc = now,
            AutoCompleteDeadlineUtc = deadline,
        };
    }
}
