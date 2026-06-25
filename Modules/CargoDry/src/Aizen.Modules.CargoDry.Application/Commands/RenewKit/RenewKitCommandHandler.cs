using Aizen.Core.Cache.Abstraction;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Message;
using Aizen.Modules.CargoDry.Application.Queries.GetCargoDryAnalytics;
using Aizen.Modules.CargoDry.Domain.Entities;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.CargoDry.Application.Commands.RenewKit;

public sealed class RenewKitCommandHandler : AizenCommandHandler<RenewKitCommand, CargoDryKitDto>
{
    private readonly ICargoDryKitRepository     _kits;
    private readonly ICargoDryProductRepository _products;
    private readonly IAizenMessagePublisher     _publisher;
    private readonly IAizenDistributedCache     _cache;

    public RenewKitCommandHandler(
        ICargoDryKitRepository kits,
        ICargoDryProductRepository products,
        IAizenMessagePublisher publisher,
        IAizenDistributedCache cache)
    {
        _kits      = kits;
        _products  = products;
        _publisher = publisher;
        _cache     = cache;
    }

    public override async Task<CargoDryKitDto?> Handle(RenewKitCommand request, CancellationToken ct)
    {
        var kit = await _kits.GetByIdAsync(request.KitId, ct)
            ?? throw new InvalidOperationException($"Kit {request.KitId} not found");

        var product = await _products.GetByCodeAsync(kit.ProductCode, ct);

        kit.Renew(request.AddedDays, request.PaymentRef ?? string.Empty);

        _ = CargoDryRenewalEntity.Create(
            kit.Id, kit.OwnerUserId!.Value,
            kit.ExpiresAt!.Value, request.AddedDays,
            request.Type, request.PaymentRef, request.AdminUserId);

        await _kits.SaveChangesAsync(ct);

        await _cache.RemoveAsync<CargoDryStatsDto>("cargodry:stats:global", ct);
        await _cache.RemoveAsync<GetCargoDryAnalyticsResponse>("cargodry:analytics:snapshot", ct);

        await _publisher.PublishAsync(new CargoDryKitRenewedMessage
        {
            KitId        = kit.Id,
            KitCode      = kit.KitCode,
            OwnerUserId  = kit.OwnerUserId!.Value,
            NewExpiresAt = kit.ExpiresAt!.Value,
            RenewalType  = request.Type.ToString(),
        }, ct);

        return new CargoDryKitDto
        {
            Id                = kit.Id,
            SerialNumber      = kit.SerialNumber,
            KitCode           = kit.KitCode,
            ProductCode       = kit.ProductCode,
            ProductName       = product?.Name ?? kit.ProductCode,
            BatchCode         = kit.BatchCode,
            Status            = kit.Status,
            OwnerUserId       = kit.OwnerUserId,
            VesselId          = kit.VesselId,
            ActivatedAt       = kit.ActivatedAt,
            ExpiresAt         = kit.ExpiresAt,
            EfficiencyPercent = kit.EfficiencyPercent,
            DaysUntilExpiry   = kit.DaysUntilExpiry,
            RenewalCount      = kit.RenewalCount,
            ManufacturedAt    = kit.ManufacturedAt,
        };
    }
}
