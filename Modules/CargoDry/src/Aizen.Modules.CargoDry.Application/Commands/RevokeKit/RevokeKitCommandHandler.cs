using Aizen.Core.Cache.Abstraction;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Abstraction.Message;
using Aizen.Modules.CargoDry.Domain.Entities;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;
using Aizen.Modules.CargoDry.Domain.MongoDocuments;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.CargoDry.Application.Commands.RevokeKit;

public sealed class RevokeKitCommandHandler : AizenCommandHandler<RevokeKitCommand, RevokeKitResponse>
{
    private readonly ICargoDryKitRepository                _kits;
    private readonly ICargoDryKitLifecycleEventRepository  _lifecycleEvents;
    private readonly IAizenMessagePublisher                _publisher;
    private readonly ICargoDryActivationLogRepository      _activationLogs;
    private readonly IAizenDistributedCache                _cache;
    private readonly ILogger<RevokeKitCommandHandler>      _logger;

    public RevokeKitCommandHandler(
        ICargoDryKitRepository               kits,
        ICargoDryKitLifecycleEventRepository lifecycleEvents,
        IAizenMessagePublisher               publisher,
        ICargoDryActivationLogRepository     activationLogs,
        IAizenDistributedCache               cache,
        ILogger<RevokeKitCommandHandler>     logger)
    {
        _kits            = kits;
        _lifecycleEvents = lifecycleEvents;
        _publisher       = publisher;
        _activationLogs  = activationLogs;
        _cache           = cache;
        _logger          = logger;
    }

    public override async Task<RevokeKitResponse> Handle(RevokeKitCommand request, CancellationToken ct)
    {
        var kit = await _kits.GetByIdAsync(request.KitId, ct)
            ?? throw new InvalidOperationException($"Kit {request.KitId} not found");

        var previousStatus = kit.Status.ToString();

        kit.Revoke(request.Reason);
        await _kits.SaveChangesAsync(ct);

        // ── Phase 9: SQL lifecycle event ──────────────────────────────────────
        var lifecycleEvent = CargoDryKitLifecycleEventEntity.Create(
            kitId:          kit.Id,
            kitCode:        kit.KitCode,
            serialNumber:   kit.SerialNumber,
            batchCode:      kit.BatchCode,
            productCode:    kit.ProductCode,
            eventType:      CargoDryKitLifecycleEventType.Revoked,
            previousStatus: previousStatus,
            newStatus:      kit.Status.ToString(),
            actorUserId:    request.AdminUserId,
            actorType:      "Admin",
            reason:         request.Reason);

        await _lifecycleEvents.AddAsync(lifecycleEvent, ct);
        await _lifecycleEvents.SaveChangesAsync(ct);

        await _cache.RemoveAsync<CargoDryStatsDto>("cargodry:stats:global", ct);

        // ── MongoDB activation log (existing, fire-and-forget) ────────────────
        var logDoc = new CargoDryActivationLogDocument
        {
            KitId        = kit.Id,
            SerialNumber = kit.SerialNumber,
            KitCode      = kit.KitCode,
            ProductCode  = kit.ProductCode,
            BatchCode    = kit.BatchCode,
            EventType    = "Revoked",
            OwnerUserId  = kit.OwnerUserId,
            VesselId     = kit.VesselId,
            RevokeReason = request.Reason,
            OccurredAt   = DateTimeOffset.UtcNow,
            DateKey      = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd"),
        };

        _ = _activationLogs.InsertAsync(logDoc, ct)
            .ContinueWith(
                t => _logger.LogError(t.Exception, "Failed to write revoke log for Kit {KitId}", kit.Id),
                TaskContinuationOptions.OnlyOnFaulted);

        await _publisher.PublishAsync(new CargoDryKitRevokedMessage
        {
            KitId       = kit.Id,
            KitCode     = kit.KitCode,
            OwnerUserId = kit.OwnerUserId,
            VesselId    = kit.VesselId,
            Reason      = request.Reason,
        }, ct);

        return new RevokeKitResponse
        {
            KitId        = kit.Id,
            KitCode      = kit.KitCode,
            SerialNumber = kit.SerialNumber,
            Status       = kit.Status.ToString(),
            Reason       = request.Reason,
            RevokedAt    = DateTimeOffset.UtcNow,
        };
    }
}
