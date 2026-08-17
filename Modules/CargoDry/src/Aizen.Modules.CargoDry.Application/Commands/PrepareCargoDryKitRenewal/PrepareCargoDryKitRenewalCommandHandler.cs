using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Domain.Entities;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.CargoDry.Application.Commands.PrepareCargoDryKitRenewal;

[DocumentationInfo("PrepareCargoDryKitRenewalCommandHandler",
    "Creates a new CargoDryRenewalPreparationEntity for admin-driven kit renewal. " +
    "Enforces one-open-preparation-per-kit rule. " +
    "Pricing is sourced from CargoDryProductEntity.RetailPrice — fails if no product found. " +
    "Does NOT create a PaymentTransaction. Does NOT call Iyzico. " +
    "Phase 11 (July 2026).")]
public sealed class PrepareCargoDryKitRenewalCommandHandler
    : AizenCommandHandler<PrepareCargoDryKitRenewalCommand, CargoDryRenewalPreparationDto>
{
    private readonly ICargoDryKitRepository                _kits;
    private readonly ICargoDryProductRepository            _products;
    private readonly ICargoDryRenewalPreparationRepository _preparations;
    private readonly IAizenInfoAccessor                    _info;
    private readonly ILogger<PrepareCargoDryKitRenewalCommandHandler> _logger;

    public PrepareCargoDryKitRenewalCommandHandler(
        ICargoDryKitRepository                           kits,
        ICargoDryProductRepository                       products,
        ICargoDryRenewalPreparationRepository            preparations,
        IAizenInfoAccessor                               info,
        ILogger<PrepareCargoDryKitRenewalCommandHandler> logger)
    {
        _kits         = kits;
        _products     = products;
        _preparations = preparations;
        _info         = info;
        _logger       = logger;
    }

    public override async Task<CargoDryRenewalPreparationDto> Handle(
        PrepareCargoDryKitRenewalCommand request, CancellationToken ct)
    {
        // ── Load kit ──────────────────────────────────────────────────────────
        var kit = await _kits.GetByIdAsync(request.KitId, ct)
            ?? throw new AizenBusinessException($"Kit {request.KitId} not found.");

        if (kit.Status == CargoDryKitStatus.Revoked)
            throw new AizenBusinessException(
                $"Kit {kit.KitCode} is revoked — renewal preparation is not allowed.");

        // ── One-open-preparation-per-kit rule ─────────────────────────────────
        var existing = await _preparations.GetOpenForKitAsync(kit.Id, ct);
        if (existing is not null)
            throw new AizenBusinessException(
                $"Kit {kit.KitCode} already has an open renewal preparation " +
                $"(RenewalCode={existing.RenewalCode}, Status={existing.Status}). " +
                "Cancel or complete the existing preparation before creating a new one.");

        // ── Pricing source: CargoDryProductEntity.RetailPrice ─────────────────
        var product = await _products.GetByCodeAsync(kit.ProductCode, ct)
            ?? throw new AizenBusinessException(
                $"Product {kit.ProductCode} not found — cannot determine renewal price. " +
                "Hard rule: do not invent renewal price if no product/price source exists.");

        if (product.RetailPrice <= 0)
            throw new AizenBusinessException(
                $"Product {kit.ProductCode} has RetailPrice={product.RetailPrice} — " +
                "cannot create renewal preparation with zero or negative price.");

        var currencyCode = !string.IsNullOrWhiteSpace(product.CurrencyCode)
            ? product.CurrencyCode
            : "TRY";

        // ── Renewal months validation ─────────────────────────────────────────
        if (request.RequestedRenewalMonths < 1 || request.RequestedRenewalMonths > 24)
            throw new AizenBusinessException(
                $"RequestedRenewalMonths={request.RequestedRenewalMonths} is out of range [1-24].");

        // ── Estimate new expiry ───────────────────────────────────────────────
        var baseExpiry      = kit.ExpiresAt ?? DateTimeOffset.UtcNow;
        var renewalDays     = request.RequestedRenewalMonths * 30;
        var newExpiresAtUtc = baseExpiry.AddDays(renewalDays);

        var renewalCode = $"RNW-{kit.KitCode}-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}";

        var preparation = CargoDryRenewalPreparationEntity.Create(
            renewalCode:           renewalCode,
            kitId:                 kit.Id,
            kitCode:               kit.KitCode,
            productCode:           kit.ProductCode,
            ownerUserId:           kit.OwnerUserId,
            vesselId:              kit.VesselId,
            providerProfileId:     kit.ProviderProfileId,
            currentExpiresAtUtc:   baseExpiry,
            requestedRenewalMonths: request.RequestedRenewalMonths,
            renewalPrice:          product.RetailPrice,
            currencyCode:          currencyCode,
            preparedByUserId:      _info.UserInfoAccessor.UserInfo.UserId,
            note:                  request.Note);

        preparation.SetNewExpiryEstimate(newExpiresAtUtc);

        await _preparations.AddAsync(preparation, ct);
        await _preparations.SaveChangesAsync(ct);

        _logger.LogInformation(
            "CargoDry renewal preparation created. KitId={KitId} KitCode={KitCode} " +
            "RenewalCode={RenewalCode} Months={Months} Price={Price}{Currency}",
            kit.Id, kit.KitCode, renewalCode,
            request.RequestedRenewalMonths, product.RetailPrice, currencyCode);

        return MapToDto(preparation, product.Name);
    }

    internal static CargoDryRenewalPreparationDto MapToDto(
        CargoDryRenewalPreparationEntity e,
        string? productName = null)
        => new()
        {
            Id                           = e.Id,
            RenewalCode                  = e.RenewalCode,
            KitId                        = e.KitId,
            KitCode                      = e.KitCode,
            ProductCode                  = e.ProductCode,
            ProductName                  = productName,
            OwnerUserId                  = e.OwnerUserId,
            VesselId                     = e.VesselId,
            ProviderProfileId            = e.ProviderProfileId,
            CurrentExpiresAtUtc          = e.CurrentExpiresAtUtc,
            RequestedRenewalMonths       = e.RequestedRenewalMonths,
            NewExpiresAtUtc              = e.NewExpiresAtUtc,
            RenewalPrice                 = e.RenewalPrice,
            CurrencyCode                 = e.CurrencyCode,
            Status                       = e.Status,
            InvoiceId                    = e.InvoiceId,
            PaymentTransactionId         = e.PaymentTransactionId,
            ManualPaymentReference       = e.ManualPaymentReference,
            NotificationStatus           = e.NotificationStatus,
            NotificationCorrelationId    = e.NotificationCorrelationId,
            NotificationChannels         = e.NotificationChannels,
            LastNotificationTemplateCode = e.LastNotificationTemplateCode,
            LastNotificationLanguageCode = e.LastNotificationLanguageCode,
            NotificationPreparedAtUtc    = e.NotificationPreparedAtUtc,
            NotificationDispatchedAtUtc  = e.NotificationDispatchedAtUtc,
            NotificationFailureReason    = e.NotificationFailureReason,
            PreparedByUserId             = e.PreparedByUserId,
            PreparedAtUtc                = e.PreparedAtUtc,
            CompletedByUserId            = e.CompletedByUserId,
            CompletedAtUtc               = e.CompletedAtUtc,
            CancelledByUserId            = e.CancelledByUserId,
            CancelledAtUtc               = e.CancelledAtUtc,
            CancellationReason           = e.CancellationReason,
            Note                         = e.Note,
            CanPrepareInvoice            = e.CanPrepareInvoice,
            CanDispatchNotification      = e.CanDispatchNotification,
            CanComplete                  = e.CanComplete,
        };
}
