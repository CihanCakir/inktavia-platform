using Aizen.Core.Cache.Abstraction;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Abstraction.Message;
using Aizen.Modules.CargoDry.Application.Queries.GetCargoDryAnalytics;
using Aizen.Modules.CargoDry.Domain.Entities;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;
using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.Model;
using Aizen.Modules.Payment.Abstraction.RemoteCall;
using Aizen.Modules.Payment.Abstraction.RemoteCall.Requests;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Aizen.Modules.CargoDry.Application.Commands.RenewKit;

public sealed class RenewKitCommandHandler : AizenCommandHandler<RenewKitCommand, CargoDryKitDto>
{
    private readonly ICargoDryKitRepository               _kits;
    private readonly ICargoDryProductRepository           _products;
    private readonly ICargoDryKitLifecycleEventRepository _lifecycleEvents;
    private readonly IAizenMessagePublisher               _publisher;
    private readonly IAizenDistributedCache               _cache;
    private readonly IPaymentModuleRemoteCall             _paymentRemoteCall;
    private readonly IAizenInfoAccessor                   _info;
    private readonly ILogger<RenewKitCommandHandler>      _logger;

    public RenewKitCommandHandler(
        ICargoDryKitRepository               kits,
        ICargoDryProductRepository           products,
        ICargoDryKitLifecycleEventRepository lifecycleEvents,
        IAizenMessagePublisher               publisher,
        IAizenDistributedCache               cache,
        IPaymentModuleRemoteCall             paymentRemoteCall,
        IAizenInfoAccessor                   info,
        ILogger<RenewKitCommandHandler>      logger)
    {
        _kits              = kits;
        _products          = products;
        _lifecycleEvents   = lifecycleEvents;
        _publisher         = publisher;
        _cache             = cache;
        _paymentRemoteCall = paymentRemoteCall;
        _info              = info;
        _logger            = logger;
    }

    public override async Task<CargoDryKitDto?> Handle(RenewKitCommand request, CancellationToken ct)
    {
        var kit = await _kits.GetByIdAsync(request.KitId, ct)
            ?? throw new InvalidOperationException($"Kit {request.KitId} not found");

        var product        = await _products.GetByCodeAsync(kit.ProductCode, ct);
        var previousStatus = kit.Status.ToString();
        var previousExpiry = kit.ExpiresAt;

        // ── Payment escrow — only for online purchases ────────────────────────
        // AdminExtension and PhysicalKit do not go through the payment gateway.
        // EscrowRequired = false: CargoDry renewals settle immediately (no hold).
        // RecipientProfileId = null: funds go to the platform, not a provider.
        var paymentRef  = request.PaymentRef;
        var adminUserId = (long?)_info.UserInfoAccessor.UserInfo.UserId;

        if (request.Type == RenewalType.OnlinePurchase && string.IsNullOrEmpty(paymentRef))
        {
            if (kit.OwnerUserId is null)
                throw new InvalidOperationException(
                    $"Kit {kit.Id} has no owner — cannot initiate renewal payment.");

            if (product is null)
                throw new InvalidOperationException(
                    $"Product {kit.ProductCode} not found — cannot determine renewal price.");

            var idempotencyKey = $"CARGODRY-RENEWAL-{kit.Id}-{DateTime.UtcNow:yyyyMMdd}";
            var rawToken       = _info.UserInfoAccessor.UserInfo.AccessToken;

            try
            {
                var escrowResult = await _paymentRemoteCall.CreateEscrowAsync(
                    new CreateEscrowRemoteCallRequest
                    {
                        IdempotencyKey     = idempotencyKey,
                        Context            = TransactionContext.ForCargoDry(kit.Id),
                        TransactionType    = TransactionType.CargoDryRenewal,
                        PayerProfileId     = kit.OwnerUserId.Value,
                        RecipientProfileId = null,              // platform payment
                        GrossAmount        = product.RetailPrice,
                        DiscountAmount     = 0m,
                        CurrencyCode       = product.CurrencyCode,
                        ProviderPlanId     = null,
                        CategoryCode       = null,
                        EscrowRequired     = false,             // immediate settle
                    },
                    authorization: $"Bearer {rawToken}",
                    cancellationToken: ct);

                paymentRef = escrowResult.TransactionCode;

                _logger.LogInformation(
                    "CargoDry renewal payment created. KitId={KitId} TransactionCode={Code} Amount={Amount} {Currency}",
                    kit.Id, escrowResult.TransactionCode, product.RetailPrice, product.CurrencyCode);
            }
            catch (Exception ex)
            {
                // Log and re-throw — renewal must not proceed without a valid payment reference.
                _logger.LogError(ex,
                    "CargoDry renewal payment failed. KitId={KitId} IdempotencyKey={Key}",
                    kit.Id, idempotencyKey);
                throw new InvalidOperationException(
                    $"Renewal payment creation failed for kit {kit.Id}.", ex);
            }
        }

        kit.Renew(request.AddedDays, paymentRef ?? string.Empty);

        _ = CargoDryRenewalEntity.Create(
            kit.Id, kit.OwnerUserId!.Value,
            kit.ExpiresAt!.Value, request.AddedDays,
            request.Type, paymentRef, adminUserId);

        await _kits.SaveChangesAsync(ct);

        // ── Phase 9: SQL lifecycle event ──────────────────────────────────────
        var eventType = request.Type == RenewalType.AdminExtension
            ? CargoDryKitLifecycleEventType.Extended
            : CargoDryKitLifecycleEventType.Renewed;

        var metadata = JsonSerializer.Serialize(new
        {
            AddedDays      = request.AddedDays,
            RenewalType    = request.Type.ToString(),
            PreviousExpiry = previousExpiry?.ToString("O"),
            NewExpiry      = kit.ExpiresAt?.ToString("O"),
            PaymentRef     = paymentRef,
        });

        var lifecycleEvent = CargoDryKitLifecycleEventEntity.Create(
            kitId:          kit.Id,
            kitCode:        kit.KitCode,
            serialNumber:   kit.SerialNumber,
            batchCode:      kit.BatchCode,
            productCode:    kit.ProductCode,
            eventType:      eventType,
            previousStatus: previousStatus,
            newStatus:      kit.Status.ToString(),
            actorUserId:    adminUserId,
            actorType:      adminUserId.HasValue ? "Admin" : "System",
            metadataJson:   metadata);

        await _lifecycleEvents.AddAsync(lifecycleEvent, ct);
        await _lifecycleEvents.SaveChangesAsync(ct);

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
            Id                   = kit.Id,
            SerialNumber         = kit.SerialNumber,
            KitCode              = kit.KitCode,
            ProductCode          = kit.ProductCode,
            ProductName          = product?.Name ?? kit.ProductCode,
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
            // Phase 0 commercial fields — retained on renewal (Decision N10/N12)
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
