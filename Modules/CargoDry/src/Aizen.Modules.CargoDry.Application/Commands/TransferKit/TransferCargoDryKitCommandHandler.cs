using Aizen.Core.Cache.Abstraction;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.CargoDry.Application.Commands.TransferKit;

public sealed class TransferCargoDryKitCommandHandler
    : AizenCommandHandler<TransferCargoDryKitCommand, TransferCargoDryKitResponse>
{
    private readonly ICargoDryKitRepository                  _kits;
    private readonly IAizenDistributedCache                  _cache;
    private readonly ILogger<TransferCargoDryKitCommandHandler> _logger;

    public TransferCargoDryKitCommandHandler(
        ICargoDryKitRepository kits,
        IAizenDistributedCache cache,
        ILogger<TransferCargoDryKitCommandHandler> logger)
    {
        _kits   = kits;
        _cache  = cache;
        _logger = logger;
    }

    public override async Task<TransferCargoDryKitResponse> Handle(
        TransferCargoDryKitCommand request, CancellationToken ct)
    {
        var kit = await _kits.GetByIdAsync(request.KitId, ct)
            ?? throw new InvalidOperationException($"Kit {request.KitId} not found.");

        // Transfer() throws InvalidOperationException if kit is not Activated
        kit.Transfer(request.NewUserId, request.NewVesselId);
        await _kits.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Kit {KitId} transferred to UserId={NewUserId} / VesselId={NewVesselId} by Admin={AdminId}.",
            kit.Id, request.NewUserId, request.NewVesselId, request.AdminId);

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
