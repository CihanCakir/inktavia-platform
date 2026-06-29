using Aizen.Core.Cache.Abstraction;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.CargoDry.Application.Commands.RevokeBatch;

public sealed class RevokeCargoDryBatchCommandHandler
    : AizenCommandHandler<RevokeCargoDryBatchCommand, RevokeCargoDryBatchResponse>
{
    private readonly ICargoDryBatchRepository           _batches;
    private readonly ICargoDryKitRepository             _kits;
    private readonly IAizenDistributedCache             _cache;
    private readonly ILogger<RevokeCargoDryBatchCommandHandler> _logger;

    public RevokeCargoDryBatchCommandHandler(
        ICargoDryBatchRepository batches,
        ICargoDryKitRepository kits,
        IAizenDistributedCache cache,
        ILogger<RevokeCargoDryBatchCommandHandler> logger)
    {
        _batches = batches;
        _kits    = kits;
        _cache   = cache;
        _logger  = logger;
    }

    public override async Task<RevokeCargoDryBatchResponse> Handle(
        RevokeCargoDryBatchCommand request, CancellationToken ct)
    {
        var batch = await _batches.GetByCodeAsync(request.BatchCode, ct)
            ?? throw new InvalidOperationException($"Batch '{request.BatchCode}' not found.");

        if (batch.IsRevoked)
            throw new InvalidOperationException(
                $"Batch '{request.BatchCode}' is already revoked.");

        // 1. Revoke the batch itself
        batch.Revoke(request.Reason);
        await _batches.SaveChangesAsync(ct);

        // 2. Cascade: revoke all Available (un-activated) kits in this batch
        var availableKits = await _kits.GetAvailableByBatchCodeAsync(request.BatchCode, ct);
        foreach (var kit in availableKits)
            kit.Revoke(request.Reason);

        if (availableKits.Count > 0)
        {
            await _kits.SaveChangesAsync(ct);
            _logger.LogInformation(
                "Batch {BatchCode} revoked — {Count} Available kit(s) also revoked.",
                request.BatchCode, availableKits.Count);
        }

        // 3. Invalidate caches that aggregate kit/batch counts
        await _cache.RemoveAsync<CargoDryStatsDto>("cargodry:stats:global", ct);
        await _cache.RemoveAsync<object>("cargodry:analytics:snapshot", ct);

        return new RevokeCargoDryBatchResponse
        {
            BatchId                  = batch.Id,
            BatchCode                = batch.BatchCode,
            Reason                   = request.Reason,
            AvailableKitsAlsoRevoked = availableKits.Count,
            RevokedAt                = batch.RevokedAt?.ToString("O") ?? DateTimeOffset.UtcNow.ToString("O"),
        };
    }
}
