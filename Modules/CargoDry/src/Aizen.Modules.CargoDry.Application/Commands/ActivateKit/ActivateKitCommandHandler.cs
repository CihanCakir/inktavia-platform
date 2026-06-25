using Aizen.Core.CQRS.Handler;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Interface.Service;
using Aizen.Modules.CargoDry.Abstraction.Message;
using Aizen.Modules.CargoDry.Domain.Entities;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Commands.ActivateKit;

public sealed class ActivateKitCommandHandler
    : AizenCommandHandler<ActivateKitCommand, CargoDryKitDto>
{
    private readonly IActivationTokenService    _tokenService;
    private readonly ICargoDryKitRepository     _kits;
    private readonly ICargoDryProductRepository _products;
    private readonly IAizenMessagePublisher     _publisher;

    public ActivateKitCommandHandler(
        IActivationTokenService tokenService,
        ICargoDryKitRepository kits,
        ICargoDryProductRepository products,
        IAizenMessagePublisher publisher)
    {
        _tokenService = tokenService;
        _kits         = kits;
        _products     = products;
        _publisher    = publisher;
    }

    public override async Task<CargoDryKitDto?> Handle(ActivateKitCommand request, CancellationToken ct)
    {
        var claims = _tokenService.Verify(request.ActivationToken)
            ?? throw new InvalidOperationException("Invalid or expired activation token");

        var kit = await _kits.GetBySerialAsync(claims.SerialNumber, ct)
            ?? throw new InvalidOperationException($"Kit not found: {claims.SerialNumber}");

        var product = await _products.GetByCodeAsync(kit.ProductCode, ct)
            ?? throw new InvalidOperationException($"Product not found: {kit.ProductCode}");

        var existingActiveKit = await _kits.GetActiveByVesselAsync(request.VesselId, kit.ProductCode, ct);
        existingActiveKit?.MarkExpired();

        kit.Activate(request.UserId, request.VesselId, product.ValidityDays);

        var log = CargoDryActivationLogEntity.Create(
            kit.Id, request.UserId, request.VesselId,
            request.Method, request.Source,
            request.DeviceInfo, request.IpAddress);

        await _kits.SaveChangesAsync(ct);

        await _publisher.PublishAsync(new CargoDryKitActivatedMessage
        {
            KitId        = kit.Id,
            KitCode      = kit.KitCode,
            SerialNumber = kit.SerialNumber,
            ProductName  = product.Name,
            OwnerUserId  = request.UserId,
            VesselId     = request.VesselId,
            ActivatedAt  = kit.ActivatedAt!.Value,
            ExpiresAt    = kit.ExpiresAt!.Value,
            ValidityDays = product.ValidityDays,
        }, ct);

        return new CargoDryKitDto
        {
            Id                = kit.Id,
            SerialNumber      = kit.SerialNumber,
            KitCode           = kit.KitCode,
            ProductCode       = kit.ProductCode,
            ProductName       = product.Name,
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
