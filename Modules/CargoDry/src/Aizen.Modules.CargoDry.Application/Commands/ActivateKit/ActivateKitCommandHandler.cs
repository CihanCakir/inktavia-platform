using Aizen.Core.Cache.Abstraction;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Interface.Service;
using Aizen.Modules.CargoDry.Abstraction.Message;
using Aizen.Modules.CargoDry.Application.Queries.GetCargoDryAnalytics;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;
using Aizen.Modules.CargoDry.Domain.MongoDocuments;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.CargoDry.Application.Commands.ActivateKit;

public sealed class ActivateKitCommandHandler
    : AizenCommandHandler<ActivateKitCommand, CargoDryKitDto>
{
    private readonly IActivationTokenService              _tokenService;
    private readonly ICargoDryKitRepository               _kits;
    private readonly ICargoDryProductRepository           _products;
    private readonly IAizenMessagePublisher               _publisher;
    private readonly ICargoDryActivationLogRepository     _activationLogs;
    private readonly IAizenDistributedCache               _cache;
    private readonly ILogger<ActivateKitCommandHandler>   _logger;
    private readonly ICargoDryCommercialActivationService _commercialActivation;

    public ActivateKitCommandHandler(
        IActivationTokenService               tokenService,
        ICargoDryKitRepository                kits,
        ICargoDryProductRepository            products,
        IAizenMessagePublisher                publisher,
        ICargoDryActivationLogRepository      activationLogs,
        IAizenDistributedCache                cache,
        ILogger<ActivateKitCommandHandler>    logger,
        ICargoDryCommercialActivationService  commercialActivation)
    {
        _tokenService         = tokenService;
        _kits                 = kits;
        _products             = products;
        _publisher            = publisher;
        _activationLogs       = activationLogs;
        _cache                = cache;
        _logger               = logger;
        _commercialActivation = commercialActivation;
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

        // ── Phase 3: resolve commercial attribution (stages entities, does not SaveChanges) ──
        await _commercialActivation.ResolveAsync(kit.Id, request.UserId, ct);

        await _kits.SaveChangesAsync(ct);

        await _cache.RemoveAsync<CargoDryStatsDto>("cargodry:stats:global", ct);
        await _cache.RemoveAsync<GetCargoDryAnalyticsResponse>("cargodry:analytics:snapshot", ct);

        var logDoc = new CargoDryActivationLogDocument
        {
            KitId            = kit.Id,
            SerialNumber     = kit.SerialNumber,
            KitCode          = kit.KitCode,
            ProductCode      = kit.ProductCode,
            BatchCode        = kit.BatchCode,
            EventType        = "Activated",
            OwnerUserId      = request.UserId,
            VesselId         = request.VesselId,
            ActivationMethod = request.Method.ToString(),
            ActivationSource = request.Source.ToString(),
            DeviceInfo       = request.DeviceInfo,
            IpAddress        = request.IpAddress,
            ExpiresAt        = kit.ExpiresAt,
            OccurredAt       = DateTimeOffset.UtcNow,
            DateKey          = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd"),
        };

        _ = _activationLogs.InsertAsync(logDoc, ct)
            .ContinueWith(
                t => _logger.LogError(t.Exception, "Failed to write activation log for Kit {KitId}", kit.Id),
                TaskContinuationOptions.OnlyOnFaulted);

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
            Id                   = kit.Id,
            SerialNumber         = kit.SerialNumber,
            KitCode              = kit.KitCode,
            ProductCode          = kit.ProductCode,
            ProductName          = product.Name,
            BatchCode            = kit.BatchCode,
            Status               = kit.Status,
            OwnerUserId          = kit.OwnerUserId,
            VesselId             = kit.VesselId,
            ActivatedAt          = kit.ActivatedAt,
            ExpiresAt            = kit.ExpiresAt,
            EfficiencyPercent    = kit.EfficiencyPercent,
            DaysUntilExpiry      = kit.DaysUntilExpiry,
            RenewalCount         = kit.RenewalCount,
            ManufacturedAt       = kit.ManufacturedAt,
            // Phase 0 commercial fields
            ProviderProfileId    = kit.ProviderProfileId,
            SalesChannel         = kit.SalesChannel,
            CommercialModel      = kit.CommercialModel,
            StockLocationType    = kit.StockLocationType,
            InvoiceId            = kit.InvoiceId,
            PaymentTransactionId = kit.PaymentTransactionId,
            // Phase 0 addendum
            WarehouseId          = kit.WarehouseId,
        };
    }
}
