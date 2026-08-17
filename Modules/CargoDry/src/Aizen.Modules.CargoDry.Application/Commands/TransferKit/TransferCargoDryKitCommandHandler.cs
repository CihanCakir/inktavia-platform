using Aizen.Core.Cache.Abstraction;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Domain.Entities;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Aizen.Modules.CargoDry.Application.Commands.TransferKit;

public sealed class TransferCargoDryKitCommandHandler
    : AizenCommandHandler<TransferCargoDryKitCommand, TransferCargoDryKitResponse>
{
    private readonly ICargoDryKitRepository                      _kits;
    private readonly ICargoDryKitLifecycleEventRepository        _lifecycleEvents;
    private readonly IAizenDistributedCache                      _cache;
    private readonly IAizenInfoAccessor                          _info;
    private readonly ILogger<TransferCargoDryKitCommandHandler>  _logger;

    public TransferCargoDryKitCommandHandler(
        ICargoDryKitRepository                     kits,
        ICargoDryKitLifecycleEventRepository       lifecycleEvents,
        IAizenDistributedCache                     cache,
        IAizenInfoAccessor                         info,
        ILogger<TransferCargoDryKitCommandHandler> logger)
    {
        _kits            = kits;
        _lifecycleEvents = lifecycleEvents;
        _cache           = cache;
        _info            = info;
        _logger          = logger;
    }

    public override async Task<TransferCargoDryKitResponse> Handle(
        TransferCargoDryKitCommand request, CancellationToken ct)
    {
        var kit = await _kits.GetByIdAsync(request.KitId, ct)
            ?? throw new InvalidOperationException($"Kit {request.KitId} not found.");

        var previousStatus   = kit.Status.ToString();
        var previousUserId   = kit.OwnerUserId;
        var previousVesselId = kit.VesselId;

        // Transfer() throws InvalidOperationException if kit is not Activated
        kit.Transfer(request.NewUserId, request.NewVesselId);
        await _kits.SaveChangesAsync(ct);

        var adminId = _info.UserInfoAccessor.UserInfo.UserId;

        _logger.LogInformation(
            "Kit {KitId} transferred to UserId={NewUserId} / VesselId={NewVesselId} by Admin={AdminId}.",
            kit.Id, request.NewUserId, request.NewVesselId, adminId);

        // ── Phase 9: SQL lifecycle event ──────────────────────────────────────
        var metadata = JsonSerializer.Serialize(new
        {
            PreviousUserId   = previousUserId,
            PreviousVesselId = previousVesselId,
            NewUserId        = request.NewUserId,
            NewVesselId      = request.NewVesselId,
        });

        var lifecycleEvent = CargoDryKitLifecycleEventEntity.Create(
            kitId:          kit.Id,
            kitCode:        kit.KitCode,
            serialNumber:   kit.SerialNumber,
            batchCode:      kit.BatchCode,
            productCode:    kit.ProductCode,
            eventType:      CargoDryKitLifecycleEventType.Transferred,
            previousStatus: previousStatus,
            newStatus:      kit.Status.ToString(),
            actorUserId:    (long?)adminId,
            actorType:      "Admin",
            metadataJson:   metadata);

        await _lifecycleEvents.AddAsync(lifecycleEvent, ct);
        await _lifecycleEvents.SaveChangesAsync(ct);

        // Invalidate global stats cache (active vessel/kit counts may shift)
        await _cache.RemoveAsync<CargoDryStatsDto>("cargodry:stats:global", ct);

        return new TransferCargoDryKitResponse
        {
            KitId         = kit.Id,
            KitCode       = kit.KitCode,
            SerialNumber  = kit.SerialNumber,
            NewUserId     = kit.OwnerUserId ?? 0,
            NewVesselId   = kit.VesselId    ?? 0,
            TransferredAt = DateTimeOffset.UtcNow.ToString("O"),
        };
    }
}
