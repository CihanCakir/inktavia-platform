using Aizen.Core.Cache.Abstraction;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Message;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;
using Aizen.Modules.CargoDry.Domain.MongoDocuments;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.CargoDry.Application.Commands.RevokeKit;

public sealed class RevokeKitCommandHandler : AizenCommandHandler<RevokeKitCommand, bool>
{
    private readonly ICargoDryKitRepository           _kits;
    private readonly IAizenMessagePublisher           _publisher;
    private readonly ICargoDryActivationLogRepository _activationLogs;
    private readonly IAizenDistributedCache           _cache;
    private readonly ILogger<RevokeKitCommandHandler> _logger;

    public RevokeKitCommandHandler(
        ICargoDryKitRepository kits,
        IAizenMessagePublisher publisher,
        ICargoDryActivationLogRepository activationLogs,
        IAizenDistributedCache cache,
        ILogger<RevokeKitCommandHandler> logger)
    {
        _kits           = kits;
        _publisher      = publisher;
        _activationLogs = activationLogs;
        _cache          = cache;
        _logger         = logger;
    }

    public override async Task<bool> Handle(RevokeKitCommand request, CancellationToken ct)
    {
        var kit = await _kits.GetByIdAsync(request.KitId, ct)
            ?? throw new InvalidOperationException($"Kit {request.KitId} not found");

        kit.Revoke(request.Reason);
        await _kits.SaveChangesAsync(ct);

        await _cache.RemoveAsync<CargoDryStatsDto>("cargodry:stats:global", ct);

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

        return true;
    }
}
